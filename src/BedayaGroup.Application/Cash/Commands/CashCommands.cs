using BedayaGroup.Application.Cash.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Cash.Commands;

public record CreateCashStorageCommand(CreateCashStorageRequest Request) : IRequest<ApiResponse<CashStorageDto>>;

public class CreateCashStorageCommandValidator : AbstractValidator<CreateCashStorageCommand>
{
    public CreateCashStorageCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم الخزينة مطلوب");
        RuleFor(x => x.Request.OpeningBalance).GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون بالسالب");
    }
}

public class CreateCashStorageCommandHandler : IRequestHandler<CreateCashStorageCommand, ApiResponse<CashStorageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateCashStorageCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<CashStorageDto>> Handle(CreateCashStorageCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.Type == CashStorageType.Project && !req.ProjectId.HasValue)
        {
            return ApiResponse<CashStorageDto>.FailureResult("خزينة المشروع يجب أن ترتبط بمشروع");
        }

        string? projectName = null;
        if (req.ProjectId.HasValue)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, cancellationToken);
            if (project == null) return ApiResponse<CashStorageDto>.FailureResult("المشروع المحدد غير موجود");
            projectName = project.Name;
        }

        var storage = new CashStorage
        {
            Name = req.Name,
            Type = req.Type,
            OpeningBalance = req.OpeningBalance,
            Location = req.Location,
            ProjectId = req.ProjectId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashStorages.Add(storage);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "CashStorage", storage.Id.ToString(), null, new { storage.Name, storage.Type, storage.OpeningBalance }, cancellationToken);

        var dto = new CashStorageDto(storage.Id, storage.Name, storage.Type, storage.OpeningBalance, storage.Location, storage.ProjectId, projectName, storage.IsActive, storage.CreatedAt, storage.OpeningBalance);
        return ApiResponse<CashStorageDto>.SuccessResult(dto, "تم إضافة الخزينة بنجاح");
    }
}

public record RecordCashTransactionCommand(RecordCashTransactionRequest Request) : IRequest<ApiResponse<CashTransactionDto>>;

public class RecordCashTransactionCommandValidator : AbstractValidator<RecordCashTransactionCommand>
{
    public RecordCashTransactionCommandValidator()
    {
        RuleFor(x => x.Request.TransactionNumber).NotEmpty().WithMessage("رقم العملية مطلوب");
        RuleFor(x => x.Request.Amount).GreaterThan(0).WithMessage("قيمة العملية يجب أن تكون أكبر من صفر");
        RuleFor(x => x.Request.CashStorageId).GreaterThan(0).WithMessage("معرف الخزينة غير صحيح");
        RuleFor(x => x.Request.Description).NotEmpty().WithMessage("بيان العملية مطلوب");
    }
}

public class RecordCashTransactionCommandHandler : IRequestHandler<RecordCashTransactionCommand, ApiResponse<CashTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RecordCashTransactionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<CashTransactionDto>> Handle(RecordCashTransactionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _context.CashTransactions.AnyAsync(ct => ct.TransactionNumber == req.TransactionNumber, cancellationToken))
        {
            return ApiResponse<CashTransactionDto>.FailureResult("رقم العملية المالية مستخدم بالفعل");
        }

        var storage = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId, cancellationToken);
        if (storage == null)
        {
            return ApiResponse<CashTransactionDto>.FailureResult("الخزينة المحددة غير موجودة");
        }

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var transaction = new CashTransaction
            {
                TransactionNumber = req.TransactionNumber,
                TransactionDate = req.TransactionDate,
                Type = req.Type,
                Amount = req.Amount,
                CashStorageId = req.CashStorageId,
                ProjectId = req.ProjectId,
                ExpenseId = req.ExpenseId,
                AdvanceId = req.AdvanceId,
                Description = req.Description,
                ReferenceNumber = req.ReferenceNumber,
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Create", "CashTransaction", transaction.Id.ToString(), null, new { transaction.TransactionNumber, transaction.Amount, transaction.Type }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);
            var project = req.ProjectId.HasValue ? await _context.Projects.FindAsync(new object[] { req.ProjectId.Value }, cancellationToken) : null;

            var dto = new CashTransactionDto(
                transaction.Id,
                transaction.TransactionNumber,
                transaction.TransactionDate,
                transaction.Type,
                GetArabicTransactionTypeName(transaction.Type),
                transaction.Amount,
                transaction.CashStorageId,
                storage.Name,
                transaction.ProjectId,
                project?.Name,
                transaction.ExpenseId,
                null,
                transaction.AdvanceId,
                null,
                transaction.Description,
                transaction.ReferenceNumber,
                transaction.CreatedByUserId,
                user?.FullName ?? "",
                transaction.CreatedAt,
                transaction.Notes
            );

            return ApiResponse<CashTransactionDto>.SuccessResult(dto, "تم تسجيل العملية المالية بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string GetArabicTransactionTypeName(CashTransactionType type) => type switch
    {
        CashTransactionType.ExpensePayment => "سداد مصروف",
        CashTransactionType.CashIn => "إيراد نقدي",
        CashTransactionType.CashOut => "مصروف نقدي",
        CashTransactionType.AdvanceGiven => "صرف عهدة",
        CashTransactionType.AdvanceReturned => "رد عهدة",
        CashTransactionType.OwnerDeposit => "إيداع مالك الشركة",
        CashTransactionType.OtherIncome => "إيراد آخر",
        CashTransactionType.OtherExpense => "مصروف آخر",
        CashTransactionType.ShareholderContribution => "مساهمة مساهم",
        _ => type.ToString()
    };
}

public record RecordExpensePaymentCommand(RecordExpensePaymentRequest Request) : IRequest<ApiResponse<CashTransactionDto>>;

public class RecordExpensePaymentCommandHandler : IRequestHandler<RecordExpensePaymentCommand, ApiResponse<CashTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RecordExpensePaymentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<CashTransactionDto>> Handle(RecordExpensePaymentCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.Amount <= 0)
        {
            return ApiResponse<CashTransactionDto>.FailureResult("مبلغ السداد يجب أن يكون أكبر من صفر");
        }

        var expense = await _context.Expenses
            .Include(e => e.CashTransactions)
            .FirstOrDefaultAsync(e => e.Id == req.ExpenseId, cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException("المصروف غير موجود");
        }

        var storage = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId, cancellationToken);
        if (storage == null)
        {
            return ApiResponse<CashTransactionDto>.FailureResult("الخزينة المحددة غير موجودة");
        }

        var currentPaid = expense.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.ExpensePayment)
            .Sum(ct => ct.Amount);

        var remaining = expense.TotalAmount - currentPaid;

        if (req.Amount > remaining)
        {
            return ApiResponse<CashTransactionDto>.FailureResult($"مبلغ السداد ({req.Amount}) أكبر من المبلغ المتبقي على المصروف ({remaining})");
        }

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var transactionNumber = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var cashTx = new CashTransaction
            {
                TransactionNumber = transactionNumber,
                TransactionDate = req.PaymentDate,
                Type = CashTransactionType.ExpensePayment,
                Amount = req.Amount,
                CashStorageId = req.CashStorageId,
                ProjectId = expense.ProjectId,
                ExpenseId = expense.Id,
                Description = $"سداد لمصروف رقم: {expense.ExpenseNumber} - {expense.Description}",
                ReferenceNumber = req.ReferenceNumber,
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(cashTx);

            // Update Expense Status
            var newTotalPaid = currentPaid + req.Amount;
            if (newTotalPaid >= expense.TotalAmount)
            {
                expense.Status = ExpenseStatus.Paid;
            }
            else
            {
                expense.Status = ExpenseStatus.PartiallyPaid;
            }

            expense.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("RecordPayment", "Expense", expense.Id.ToString(), new { CurrentPaid = currentPaid }, new { NewPaid = newTotalPaid, Status = expense.Status.ToString() }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);
            var project = await _context.Projects.FindAsync(new object[] { expense.ProjectId }, cancellationToken);

            var dto = new CashTransactionDto(
                cashTx.Id,
                cashTx.TransactionNumber,
                cashTx.TransactionDate,
                cashTx.Type,
                "سداد مصروف",
                cashTx.Amount,
                cashTx.CashStorageId,
                storage.Name,
                cashTx.ProjectId,
                project?.Name,
                cashTx.ExpenseId,
                expense.ExpenseNumber,
                null,
                null,
                cashTx.Description,
                cashTx.ReferenceNumber,
                cashTx.CreatedByUserId,
                user?.FullName ?? "",
                cashTx.CreatedAt,
                cashTx.Notes
            );

            return ApiResponse<CashTransactionDto>.SuccessResult(dto, "تم تسجيل سداد المصروف بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
