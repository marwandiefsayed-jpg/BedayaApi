using BedayaGroup.Application.Cash;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.DTOs;
using BedayaGroup.Application.Storages;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Expenses.Commands;

public record CreateExpenseCommand(CreateExpenseRequest Request) : IRequest<ApiResponse<ExpenseDto>>;

public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.Request.ExpenseNumber).NotEmpty().WithMessage("رقم المصروف مطلوب");
        RuleFor(x => x.Request.TotalAmount).GreaterThan(0).WithMessage("إجمالي قيمة المصروف يجب أن تكون أكبر من صفر");
        RuleFor(x => x.Request.Description).NotEmpty().WithMessage("بيان المصروف مطلوب");
        RuleFor(x => x.Request.StorageId).GreaterThan(0).WithMessage("المخزن المحدد غير صحيح").When(x => x.Request.StorageId.HasValue);
        // ProjectId is only required when TargetAllProjects is false
        RuleFor(x => x.Request.ProjectId)
            .NotNull().WithMessage("يجب تحديد مشروع واحد على الأقل أو اختيار عدة مشاريع")
            .GreaterThan(0).WithMessage("معرف المشروع غير صحيح")
            .When(x => !x.Request.TargetAllProjects && (x.Request.ProjectIds == null || !x.Request.ProjectIds.Any()));
    }
}

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public CreateExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (string.IsNullOrWhiteSpace(req.MaterialName) || string.IsNullOrWhiteSpace(req.Unit) || req.Quantity <= 0 || req.UnitPrice <= 0)
        {
            return ApiResponse<ExpenseDto>.FailureResult("بيانات المادة ووحدة القياس والكمية وسعر الوحدة مطلوبة");
        }

        if (await _context.Expenses.AnyAsync(e => e.ExpenseNumber == req.ExpenseNumber, cancellationToken))
        {
            return ApiResponse<ExpenseDto>.FailureResult("رقم المصروف مستخدم بالفعل");
        }

        var currentUserId = _currentUserService.UserId ?? 1;
        var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);

        // ── Multi-Projects or All-Projects Mode ─────────────────────────────
        var isMultiMode = req.TargetAllProjects || (req.ProjectIds != null && req.ProjectIds.Count > 1);
        if (isMultiMode)
        {
            List<Project> targetProjects;
            if (req.TargetAllProjects)
            {
                targetProjects = await _context.Projects
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var pIds = req.ProjectIds!;
                targetProjects = await _context.Projects
                    .Where(p => pIds.Contains(p.Id) && p.IsActive)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }

            if (!targetProjects.Any())
            {
                return ApiResponse<ExpenseDto>.FailureResult("لم يتم العثور على مشاريع نشطة لإضافة المصروف عليها");
            }

            ExpenseDto? firstDto = null;
            int suffix = 1;
            foreach (var proj in targetProjects)
            {
                var expNum = targetProjects.Count > 1 ? $"{req.ExpenseNumber}-{suffix++}" : req.ExpenseNumber;
                var total = req.Quantity * req.UnitPrice;

                // Each project receives the material in its own dedicated warehouse.
                var materialStorage = await ProjectMaterialStorage.GetOrCreateAsync(_context, proj, cancellationToken);

                var expense = new Expense
                {
                    ExpenseNumber = expNum,
                    ProjectId = proj.Id,
                    StorageId = materialStorage.Id,
                    ExpenseDate = req.ExpenseDate,
                    Description = req.Description,
                    MaterialName = req.MaterialName.Trim(),
                    Unit = req.Unit.Trim(),
                    Quantity = req.Quantity,
                    UnitPrice = req.UnitPrice,
                    TotalAmount = total,
                    CreatedByUserId = currentUserId,
                    Notes = req.Notes,
                    Status = ExpenseStatus.Paid,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync(cancellationToken);

                // Each project is settled from its own dedicated cash storage
                var projectStorage = await ProjectCashStorage.GetOrCreateAsync(_context, proj, cancellationToken);

                // Create automatic payment cash transaction
                var cashTx = new CashTransaction
                {
                    TransactionNumber = $"EXP-PAY-{expense.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    TransactionDate = req.ExpenseDate,
                    Type = CashTransactionType.ExpensePayment,
                    Amount = total,
                    CashStorageId = projectStorage.Id,
                    ProjectId = proj.Id,
                    ExpenseId = expense.Id,
                    Description = $"سداد تلقائي للمصروف رقم {expense.ExpenseNumber}: {expense.Description}",
                    CreatedByUserId = currentUserId,
                    Notes = req.Notes
                };
                _context.CashTransactions.Add(cashTx);

                // Create automatic storage transaction in the project's own warehouse
                var storageTx = new StorageTransaction
                {
                    StorageId = materialStorage.Id,
                    ProjectId = proj.Id,
                    TransactionDate = req.ExpenseDate,
                    MaterialName = req.MaterialName.Trim(),
                    Unit = req.Unit.Trim(),
                    Quantity = req.Quantity,
                    Type = StorageTransactionType.Purchase,
                    ReferenceNumber = expense.ExpenseNumber,
                    Description = $"تخزين تلقائي لشراء مادة للمصروف رقم {expense.ExpenseNumber}",
                    CreatedByUserId = currentUserId
                };
                _context.StorageTransactions.Add(storageTx);

                await _context.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("Create", "Expense", expense.Id.ToString(), null,
                    new { expense.ExpenseNumber, expense.TotalAmount, expense.ProjectId, expense.StorageId },
                    cancellationToken);

                firstDto ??= new ExpenseDto(
                    expense.Id,
                    expense.ExpenseNumber,
                    expense.ProjectId,
                    proj.Name,
                    expense.StorageId,
                    materialStorage.Name,
                    expense.ExpenseDate,
                    expense.Description,
                    expense.TotalAmount,
                    expense.TotalAmount,
                    0m,
                    expense.Status,
                    currentUserId,
                    user?.FullName ?? "النظام",
                    expense.CreatedAt,
                    expense.Notes,
                    expense.MaterialName,
                    expense.Unit,
                    expense.Quantity,
                    expense.UnitPrice
                );
            }

            return ApiResponse<ExpenseDto>.SuccessResult(firstDto!);
        }

        // ── Single-Project Mode ──────────────────────────────────────────
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, cancellationToken);
        if (project == null)
        {
            return ApiResponse<ExpenseDto>.FailureResult("المشروع المحدد غير موجود");
        }

        var singleTotal = req.Quantity * req.UnitPrice;

        Storage? singleMaterialStorage = null;
        if (req.StorageId.HasValue)
        {
            singleMaterialStorage = await _context.Storages.FirstOrDefaultAsync(s => s.Id == req.StorageId.Value, cancellationToken);
            if (singleMaterialStorage == null)
            {
                return ApiResponse<ExpenseDto>.FailureResult("المخزن المحدد غير موجود");
            }
        }
        singleMaterialStorage ??= await ProjectMaterialStorage.GetOrCreateAsync(_context, project, cancellationToken);

        var singleExpense = new Expense
        {
            ExpenseNumber = req.ExpenseNumber,
            ProjectId = req.ProjectId!.Value,
            StorageId = singleMaterialStorage.Id,
            ExpenseDate = req.ExpenseDate,
            Description = req.Description,
            MaterialName = req.MaterialName.Trim(),
            Unit = req.Unit.Trim(),
            Quantity = req.Quantity,
            UnitPrice = req.UnitPrice,
            TotalAmount = singleTotal,
            CreatedByUserId = currentUserId,
            Notes = req.Notes,
            Status = ExpenseStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(singleExpense);
        await _context.SaveChangesAsync(cancellationToken);

        var singleProjectStorage = await ProjectCashStorage.GetOrCreateAsync(_context, project, cancellationToken);

        // Create automatic payment cash transaction
        var singleCashTx = new CashTransaction
        {
            TransactionNumber = $"EXP-PAY-{singleExpense.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            TransactionDate = req.ExpenseDate,
            Type = CashTransactionType.ExpensePayment,
            Amount = singleTotal,
            CashStorageId = singleProjectStorage.Id,
            ProjectId = req.ProjectId.Value,
            ExpenseId = singleExpense.Id,
            Description = $"سداد تلقائي للمصروف رقم {singleExpense.ExpenseNumber}: {singleExpense.Description}",
            CreatedByUserId = currentUserId,
            Notes = req.Notes
        };
        _context.CashTransactions.Add(singleCashTx);

        // Create automatic storage transaction in the project's own warehouse
        var singleStorageTx = new StorageTransaction
        {
            StorageId = singleMaterialStorage.Id,
            ProjectId = req.ProjectId.Value,
            TransactionDate = req.ExpenseDate,
            MaterialName = req.MaterialName.Trim(),
            Unit = req.Unit.Trim(),
            Quantity = req.Quantity,
            Type = StorageTransactionType.Purchase,
            ReferenceNumber = singleExpense.ExpenseNumber,
            Description = $"تخزين تلقائي لشراء مادة للمصروف رقم {singleExpense.ExpenseNumber}",
            CreatedByUserId = currentUserId
        };
        _context.StorageTransactions.Add(singleStorageTx);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Expense", singleExpense.Id.ToString(), null,
            new { singleExpense.ExpenseNumber, singleExpense.TotalAmount, singleExpense.ProjectId, singleExpense.StorageId },
            cancellationToken);

        var dto = new ExpenseDto(
            singleExpense.Id,
            singleExpense.ExpenseNumber,
            singleExpense.ProjectId,
            project.Name,
            singleExpense.StorageId,
            singleMaterialStorage.Name,
            singleExpense.ExpenseDate,
            singleExpense.Description,
            singleExpense.TotalAmount,
            singleExpense.TotalAmount,
            0m,
            ExpenseStatus.Paid,
            singleExpense.CreatedByUserId,
            user?.FullName ?? "",
            singleExpense.CreatedAt,
            singleExpense.Notes,
            singleExpense.MaterialName,
            singleExpense.Unit,
            singleExpense.Quantity,
            singleExpense.UnitPrice
        );

        return ApiResponse<ExpenseDto>.SuccessResult(dto, "تم تسجيل المصروف وتسديده تلقائياً بنجاح");
    }
}

public record UpdateExpenseCommand(int Id, UpdateExpenseRequest Request) : IRequest<ApiResponse<ExpenseDto>>;

public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateExpenseCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Storage)
            .Include(e => e.CreatedByUser)
            .Include(e => e.CashTransactions)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException("المصروف غير موجود");
        }

        var req = request.Request;

        if (string.IsNullOrWhiteSpace(req.MaterialName) || string.IsNullOrWhiteSpace(req.Unit) || req.Quantity <= 0 || req.UnitPrice <= 0)
        {
            return ApiResponse<ExpenseDto>.FailureResult("بيانات المادة ووحدة القياس والكمية وسعر الوحدة مطلوبة");
        }

        // Each project has one material storage; editing an expense must always use that storage.
        var storage = await ProjectMaterialStorage.GetOrCreateAsync(_context, expense.Project!, cancellationToken);

        var calculatedTotal = req.Quantity * req.UnitPrice;
        var oldValues = new { expense.TotalAmount, expense.Description, expense.StorageId };

        expense.StorageId = storage.Id;
        expense.ExpenseDate = req.ExpenseDate;
        expense.Description = req.Description;
        expense.MaterialName = req.MaterialName.Trim();
        expense.Unit = req.Unit.Trim();
        expense.Quantity = req.Quantity;
        expense.UnitPrice = req.UnitPrice;
        expense.TotalAmount = calculatedTotal;
        expense.Notes = req.Notes;
        expense.Status = ExpenseStatus.Paid;
        expense.UpdatedAt = DateTime.UtcNow;

        // Update automatic cash transaction payment amount
        var paymentTx = expense.CashTransactions.FirstOrDefault(ct => ct.Type == CashTransactionType.ExpensePayment);
        if (paymentTx != null)
        {
            paymentTx.Amount = calculatedTotal;
            paymentTx.TransactionDate = req.ExpenseDate;
            paymentTx.Description = $"سداد تلقائي للمصروف رقم {expense.ExpenseNumber}: {expense.Description}";
        }

        // Keep the automatic inventory purchase in sync with every editable expense field.
        // This prevents the expense, cash transaction, and storage balance from diverging.
        var storageTx = await _context.StorageTransactions
            .FirstOrDefaultAsync(st => st.ReferenceNumber == expense.ExpenseNumber && st.Type == StorageTransactionType.Purchase, cancellationToken);
        if (storageTx != null)
        {
            storageTx.StorageId = storage.Id;
            storageTx.ProjectId = expense.ProjectId;
            storageTx.TransactionDate = req.ExpenseDate;
            storageTx.MaterialName = req.MaterialName.Trim();
            storageTx.Unit = req.Unit.Trim();
            storageTx.Quantity = req.Quantity;
            storageTx.Description = $"تخزين تلقائي لشراء مادة للمصروف رقم {expense.ExpenseNumber}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Expense", expense.Id.ToString(), oldValues, new { expense.TotalAmount, expense.Description }, cancellationToken);

        var dto = new ExpenseDto(
            expense.Id,
            expense.ExpenseNumber,
            expense.ProjectId,
            expense.Project.Name,
            expense.StorageId,
            storage.Name,
            expense.ExpenseDate,
            expense.Description,
            expense.TotalAmount,
            expense.TotalAmount,
            0m,
            expense.Status,
            expense.CreatedByUserId,
            expense.CreatedByUser.FullName,
            expense.CreatedAt,
            expense.Notes,
            expense.MaterialName,
            expense.Unit,
            expense.Quantity,
            expense.UnitPrice
        );

        return ApiResponse<ExpenseDto>.SuccessResult(dto, "تم تحديث المصروف بنجاح");
    }
}

// =============================================
// Delete Expense Command
// =============================================

public record DeleteExpenseCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteExpenseCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _context.Expenses
            .Include(e => e.CashTransactions)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException("المصروف غير موجود");
        }

        // Remove all linked cash transactions (payments) first
        if (expense.CashTransactions.Any())
        {
            _context.CashTransactions.RemoveRange(expense.CashTransactions);
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "Delete", "Expense", expense.Id.ToString(),
            new { expense.ExpenseNumber, expense.TotalAmount, expense.StorageId, expense.ProjectId },
            null, cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف المصروف وجميع مدفوعاته بنجاح");
    }
}
