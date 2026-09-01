using BedayaGroup.Application.Advances.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Advances.Commands;

public record CreateAdvanceCommand(CreateAdvanceRequest Request) : IRequest<ApiResponse<AdvanceDto>>;

public class CreateAdvanceCommandValidator : AbstractValidator<CreateAdvanceCommand>
{
    public CreateAdvanceCommandValidator()
    {
        RuleFor(x => x.Request.AdvanceNumber).NotEmpty().WithMessage("رقم العهدة مطلوب");
        RuleFor(x => x.Request.EngineerId).GreaterThan(0).WithMessage("معرف المهندس غير صحيح");
        RuleFor(x => x.Request.ProjectId).GreaterThan(0).WithMessage("معرف المشروع غير صحيح");
        RuleFor(x => x.Request.CashStorageId).GreaterThan(0).WithMessage("معرف الخزينة غير صحيح");
        RuleFor(x => x.Request.IssuedAmount).GreaterThan(0).WithMessage("مبلغ العهدة يجب أن يكون أكبر من صفر");
    }
}

public class CreateAdvanceCommandHandler : IRequestHandler<CreateAdvanceCommand, ApiResponse<AdvanceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public CreateAdvanceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<AdvanceDto>> Handle(CreateAdvanceCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _context.Advances.AnyAsync(a => a.AdvanceNumber == req.AdvanceNumber, cancellationToken))
        {
            return ApiResponse<AdvanceDto>.FailureResult("رقم العهدة مستخدم بالفعل");
        }

        var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.Id == req.EngineerId, cancellationToken);
        if (engineer == null) return ApiResponse<AdvanceDto>.FailureResult("المهندس المحدد غير موجود");

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, cancellationToken);
        if (project == null) return ApiResponse<AdvanceDto>.FailureResult("المشروع المحدد غير موجود");

        // Business Rule validation: Engineer must be assigned to project before receiving advance
        var isAssigned = await _context.ProjectEngineers.AnyAsync(pe => pe.EngineerId == req.EngineerId && pe.ProjectId == req.ProjectId, cancellationToken);
        if (!isAssigned)
        {
            return ApiResponse<AdvanceDto>.FailureResult("لا يمكن صرف عهدة للمهندس لأنه غير مسند على هذا المشروع");
        }

        var storage = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId, cancellationToken);
        if (storage == null) return ApiResponse<AdvanceDto>.FailureResult("الخزينة المحددة غير موجودة");

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var advance = new Advance
            {
                AdvanceNumber = req.AdvanceNumber,
                EngineerId = req.EngineerId,
                ProjectId = req.ProjectId,
                IssuedAmount = req.IssuedAmount,
                IssueDate = req.IssueDate,
                Status = AdvanceStatus.Open,
                Notes = req.Notes,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Advances.Add(advance);
            await _context.SaveChangesAsync(cancellationToken);

            var txNumber = $"ADV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var cashTx = new CashTransaction
            {
                TransactionNumber = txNumber,
                TransactionDate = req.IssueDate,
                Type = CashTransactionType.AdvanceGiven,
                Amount = req.IssuedAmount,
                CashStorageId = req.CashStorageId,
                ProjectId = req.ProjectId,
                AdvanceId = advance.Id,
                Description = $"صرف عهدة رقم: {advance.AdvanceNumber} للمهندس {engineer.FullName}",
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(cashTx);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CreateAdvance", "Advance", advance.Id.ToString(), null, new { advance.AdvanceNumber, advance.IssuedAmount, advance.EngineerId }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);

            var dto = new AdvanceDto(
                advance.Id,
                advance.AdvanceNumber,
                engineer.Id,
                engineer.FullName,
                engineer.Code,
                project.Id,
                project.Name,
                advance.IssuedAmount,
                0m,
                advance.IssuedAmount,
                advance.IssueDate,
                advance.SettlementDate,
                advance.Status,
                advance.CreatedByUserId,
                user?.FullName ?? "",
                advance.CreatedAt,
                advance.Notes
            );

            return ApiResponse<AdvanceDto>.SuccessResult(dto, "تم صرف العهدة للمهندس بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record SettleAdvanceCommand(SettleAdvanceRequest Request) : IRequest<ApiResponse<AdvanceDto>>;

public class SettleAdvanceCommandHandler : IRequestHandler<SettleAdvanceCommand, ApiResponse<AdvanceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public SettleAdvanceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<AdvanceDto>> Handle(SettleAdvanceCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.SettledAmount <= 0)
        {
            return ApiResponse<AdvanceDto>.FailureResult("مبلغ التسوية يجب أن يكون أكبر من صفر");
        }

        var advance = await _context.Advances
            .Include(a => a.Engineer)
            .Include(a => a.Project)
            .Include(a => a.CreatedByUser)
            .Include(a => a.CashTransactions)
            .FirstOrDefaultAsync(a => a.Id == req.AdvanceId, cancellationToken);

        if (advance == null)
        {
            throw new NotFoundException("العهدة غير موجودة");
        }

        var currentSettled = advance.CashTransactions
            .Where(ct => ct.Type == CashTransactionType.AdvanceReturned)
            .Sum(ct => ct.Amount);

        var remaining = advance.IssuedAmount - currentSettled;

        if (req.SettledAmount > remaining)
        {
            return ApiResponse<AdvanceDto>.FailureResult($"مبلغ التسوية ({req.SettledAmount}) أكبر من المبلغ المتبقي على العهدة ({remaining})");
        }

        var storage = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId, cancellationToken);
        if (storage == null) return ApiResponse<AdvanceDto>.FailureResult("الخزينة المحددة غير موجودة");

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var txNumber = $"SET-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var cashTx = new CashTransaction
            {
                TransactionNumber = txNumber,
                TransactionDate = req.SettlementDate,
                Type = CashTransactionType.AdvanceReturned,
                Amount = req.SettledAmount,
                CashStorageId = req.CashStorageId,
                ProjectId = advance.ProjectId,
                AdvanceId = advance.Id,
                Description = $"تسوية / رد عهدة رقم: {advance.AdvanceNumber} - {req.Description}",
                ReferenceNumber = req.ReferenceNumber,
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(cashTx);

            var newSettled = currentSettled + req.SettledAmount;
            advance.SettlementDate = req.SettlementDate;
            if (newSettled >= advance.IssuedAmount)
            {
                advance.Status = AdvanceStatus.FullySettled;
            }
            else
            {
                advance.Status = AdvanceStatus.PartiallySettled;
            }

            advance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("SettleAdvance", "Advance", advance.Id.ToString(), new { CurrentSettled = currentSettled }, new { NewSettled = newSettled, Status = advance.Status.ToString() }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var dto = new AdvanceDto(
                advance.Id,
                advance.AdvanceNumber,
                advance.Engineer.Id,
                advance.Engineer.FullName,
                advance.Engineer.Code,
                advance.Project.Id,
                advance.Project.Name,
                advance.IssuedAmount,
                newSettled,
                advance.IssuedAmount - newSettled,
                advance.IssueDate,
                advance.SettlementDate,
                advance.Status,
                advance.CreatedByUserId,
                advance.CreatedByUser.FullName,
                advance.CreatedAt,
                advance.Notes
            );

            return ApiResponse<AdvanceDto>.SuccessResult(dto, "تم تسوية العهدة بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
