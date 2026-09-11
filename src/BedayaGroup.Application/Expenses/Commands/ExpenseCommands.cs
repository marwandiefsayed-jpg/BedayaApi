using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Expenses.DTOs;
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
        RuleFor(x => x.Request.ProjectId).GreaterThan(0).WithMessage("معرف المشروع غير صحيح");
        RuleFor(x => x.Request.SupplierId).GreaterThan(0).WithMessage("معرف المورد غير صحيح");
        RuleFor(x => x.Request.TotalAmount).GreaterThan(0).WithMessage("إجمالي قيمة المصروف يجب أن تكون أكبر من صفر");
        RuleFor(x => x.Request.Description).NotEmpty().WithMessage("بيان المصروف مطلوب");
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

        if (await _context.Expenses.AnyAsync(e => e.ExpenseNumber == req.ExpenseNumber, cancellationToken))
        {
            return ApiResponse<ExpenseDto>.FailureResult("رقم المصروف مستخدم بالفعل");
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, cancellationToken);
        if (project == null)
        {
            return ApiResponse<ExpenseDto>.FailureResult("المشروع المحدد غير موجود");
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == req.SupplierId, cancellationToken);
        if (supplier == null)
        {
            return ApiResponse<ExpenseDto>.FailureResult("المورد المحدد غير موجود");
        }

        var currentUserId = _currentUserService.UserId ?? 1;

        var expense = new Expense
        {
            ExpenseNumber = req.ExpenseNumber,
            ProjectId = req.ProjectId,
            SupplierId = req.SupplierId,
            ExpenseDate = req.ExpenseDate,
            Description = req.Description,
            TotalAmount = req.TotalAmount,
            CreatedByUserId = currentUserId,
            Notes = req.Notes,
            Status = ExpenseStatus.Due,
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Expense", expense.Id.ToString(), null, new { expense.ExpenseNumber, expense.TotalAmount, expense.ProjectId, expense.SupplierId }, cancellationToken);

        var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);

        var dto = new ExpenseDto(
            expense.Id,
            expense.ExpenseNumber,
            expense.ProjectId,
            project.Name,
            expense.SupplierId,
            supplier.Name,
            expense.ExpenseDate,
            expense.Description,
            expense.TotalAmount,
            0m,
            expense.TotalAmount,
            ExpenseStatus.Due,
            expense.CreatedByUserId,
            user?.FullName ?? "",
            expense.CreatedAt,
            expense.Notes
        );

        return ApiResponse<ExpenseDto>.SuccessResult(dto, "تم تسجيل المصروف بنجاح");
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
            .Include(e => e.Supplier)
            .Include(e => e.CreatedByUser)
            .Include(e => e.CashTransactions)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException("المصروف غير موجود");
        }

        var req = request.Request;

        var paidAmount = expense.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.ExpensePayment)
            .Sum(ct => ct.Amount);

        if (req.TotalAmount < paidAmount)
        {
            return ApiResponse<ExpenseDto>.FailureResult("إجمالي المبلغ الجديد أقل من المدفوعات المسجلة بالفعل على هذا المصروف");
        }

        var oldValues = new { expense.TotalAmount, expense.Description, expense.SupplierId };

        expense.SupplierId = req.SupplierId;
        expense.ExpenseDate = req.ExpenseDate;
        expense.Description = req.Description;
        expense.TotalAmount = req.TotalAmount;
        expense.Notes = req.Notes;
        expense.UpdatedAt = DateTime.UtcNow;

        var remaining = expense.TotalAmount - paidAmount;
        if (remaining == 0) expense.Status = ExpenseStatus.Paid;
        else if (paidAmount > 0) expense.Status = ExpenseStatus.PartiallyPaid;
        else expense.Status = ExpenseStatus.Due;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Expense", expense.Id.ToString(), oldValues, new { expense.TotalAmount, expense.Description }, cancellationToken);

        var dto = new ExpenseDto(
            expense.Id,
            expense.ExpenseNumber,
            expense.ProjectId,
            expense.Project.Name,
            expense.SupplierId,
            expense.Supplier.Name,
            expense.ExpenseDate,
            expense.Description,
            expense.TotalAmount,
            paidAmount,
            remaining,
            expense.Status,
            expense.CreatedByUserId,
            expense.CreatedByUser.FullName,
            expense.CreatedAt,
            expense.Notes
        );

        return ApiResponse<ExpenseDto>.SuccessResult(dto, "تم تحديث المصروف بنجاح");
    }
}
