using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shareholders.Commands;

// =============================================
// Shareholder CRUD Commands
// =============================================

public record CreateShareholderCommand(CreateShareholderRequest Request) : IRequest<ApiResponse<ShareholderDto>>;

public class CreateShareholderCommandHandler : IRequestHandler<CreateShareholderCommand, ApiResponse<ShareholderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateShareholderCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderDto>> Handle(CreateShareholderCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!req.ProjectId.HasValue)
        {
            return ApiResponse<ShareholderDto>.FailureResult("يجب ربط المساهم بمشروع");
        }

        if (!await _context.Projects.AnyAsync(p => p.Id == req.ProjectId.Value, cancellationToken))
        {
            return ApiResponse<ShareholderDto>.FailureResult("المشروع المحدد غير موجود");
        }

        if (await _context.Shareholders.AnyAsync(s => s.Code == req.Code, cancellationToken))
        {
            return ApiResponse<ShareholderDto>.FailureResult("كود المساهم مستخدم بالفعل");
        }

        int targetShareId = await _context.Shares.Select(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (targetShareId == 0)
        {
            var defaultShare = new Share
            {
                Name = "Default System Share",
                StartDate = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Shares.Add(defaultShare);
            await _context.SaveChangesAsync(cancellationToken);
            targetShareId = defaultShare.Id;
        }

        var shareholder = new Shareholder
        {
            Code = req.Code,
            Name = req.Name,
            Phone = req.Phone,
            NumberOfShares = req.NumberOfShares,
            ShareId = targetShareId,
            ProjectId = req.ProjectId,
            Notes = req.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shareholders.Add(shareholder);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Shareholder", shareholder.Id.ToString(), null, new { shareholder.Code, shareholder.Name, shareholder.NumberOfShares, shareholder.ProjectId }, cancellationToken);

        string? projectName = req.ProjectId.HasValue
            ? await _context.Projects.Where(p => p.Id == req.ProjectId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var dto = new ShareholderDto(shareholder.Id, shareholder.Code, shareholder.Name, shareholder.Phone, shareholder.NumberOfShares, shareholder.ProjectId, projectName, shareholder.Notes, shareholder.IsActive, shareholder.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto, "تم إضافة المساهم بنجاح");
    }
}

public record UpdateShareholderCommand(int Id, UpdateShareholderRequest Request) : IRequest<ApiResponse<ShareholderDto>>;

public class UpdateShareholderCommandHandler : IRequestHandler<UpdateShareholderCommand, ApiResponse<ShareholderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateShareholderCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderDto>> Handle(UpdateShareholderCommand request, CancellationToken cancellationToken)
    {
        var shareholder = await _context.Shareholders
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shareholder == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        var req = request.Request;

        if (!req.ProjectId.HasValue)
        {
            return ApiResponse<ShareholderDto>.FailureResult("يجب ربط المساهم بمشروع");
        }

        if (!await _context.Projects.AnyAsync(p => p.Id == req.ProjectId.Value, cancellationToken))
        {
            return ApiResponse<ShareholderDto>.FailureResult("المشروع المحدد غير موجود");
        }

        var oldValues = new { shareholder.Name, shareholder.NumberOfShares, shareholder.ShareId, shareholder.ProjectId };

        shareholder.Name = req.Name;
        shareholder.Phone = req.Phone;
        shareholder.NumberOfShares = req.NumberOfShares;
        shareholder.ProjectId = req.ProjectId;
        shareholder.Notes = req.Notes;
        shareholder.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Shareholder", shareholder.Id.ToString(), oldValues, new { shareholder.Name, shareholder.NumberOfShares, shareholder.ProjectId }, cancellationToken);

        string? projectName = shareholder.ProjectId.HasValue
            ? await _context.Projects.Where(p => p.Id == shareholder.ProjectId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var dto = new ShareholderDto(shareholder.Id, shareholder.Code, shareholder.Name, shareholder.Phone, shareholder.NumberOfShares, shareholder.ProjectId, projectName, shareholder.Notes, shareholder.IsActive, shareholder.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto, "تم تحديث بيانات المساهم بنجاح");
    }
}

// =============================================
// Payment Recording with Cascading Allocation
// =============================================

public record RecordShareholderContributionCommand(RecordShareholderContributionRequest Request) : IRequest<ApiResponse<ShareholderContributionDto>>;

public class RecordShareholderContributionCommandHandler : IRequestHandler<RecordShareholderContributionCommand, ApiResponse<ShareholderContributionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RecordShareholderContributionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderContributionDto>> Handle(RecordShareholderContributionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.Amount <= 0)
        {
            return ApiResponse<ShareholderContributionDto>.FailureResult("مبلغ الدفع يجب أن يكون أكبر من صفر");
        }

        var shareholder = await _context.Shareholders
            .FirstOrDefaultAsync(s => s.Id == req.ShareholderId, cancellationToken);

        if (shareholder == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        int targetStorageId = 0;
        if (req.CashStorageId.HasValue && req.CashStorageId.Value > 0)
        {
            var existing = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId.Value, cancellationToken);
            if (existing != null) targetStorageId = existing.Id;
        }

        if (targetStorageId == 0)
        {
            var anyStorage = await _context.CashStorages.FirstOrDefaultAsync(cancellationToken);
            if (anyStorage != null)
            {
                targetStorageId = anyStorage.Id;
            }
            else
            {
                var defaultStorage = new CashStorage
                {
                    Name = "الخزينة الرئيسية",
                    Type = CashStorageType.Company,
                    OpeningBalance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CashStorages.Add(defaultStorage);
                await _context.SaveChangesAsync(cancellationToken);
                targetStorageId = defaultStorage.Id;
            }
        }

        string descriptionText = !string.IsNullOrWhiteSpace(req.Description)
            ? req.Description
            : $"مساهمة من المساهم: {shareholder.Name}";

        // Validate the target installment
        var targetInstallment = await _context.ProjectInstallments
            .FirstOrDefaultAsync(i => i.Id == req.ProjectInstallmentId && i.ProjectId == req.ProjectId, cancellationToken);

        if (targetInstallment == null)
        {
            return ApiResponse<ShareholderContributionDto>.FailureResult("القسط المحدد غير موجود لهذا المشروع");
        }

        // Load all installments for this project ordered chronologically to apply cascading
        var allInstallments = await _context.ProjectInstallments
            .Where(i => i.ProjectId == req.ProjectId && i.IsActive)
            .OrderBy(i => i.EndDate)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        // Load all existing allocations for this shareholder in this project
        var existingAllocations = await _context.ShareholderPaymentAllocations
            .Include(a => a.ShareholderContribution)
            .Where(a => a.ShareholderContribution.ShareholderId == req.ShareholderId
                     && a.ShareholderContribution.ProjectId == req.ProjectId)
            .GroupBy(a => a.ProjectInstallmentId)
            .Select(g => new { InstallmentId = g.Key, TotalPaid = g.Sum(a => a.AmountAllocated) })
            .ToListAsync(cancellationToken);

        var paidByInstallment = existingAllocations.ToDictionary(x => x.InstallmentId, x => x.TotalPaid);

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create cash transaction
            var txNumber = $"SHR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var project = await _context.Projects.FindAsync(new object[] { req.ProjectId }, cancellationToken);

            var cashTx = new CashTransaction
            {
                TransactionNumber = txNumber,
                TransactionDate = req.ContributionDate,
                Type = CashTransactionType.ShareholderContribution,
                Amount = req.Amount,
                CashStorageId = targetStorageId,
                ProjectId = req.ProjectId,
                Description = descriptionText,
                ReferenceNumber = req.ReferenceNumber,
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(cashTx);
            await _context.SaveChangesAsync(cancellationToken);

            // Create shareholder contribution record linked to the target installment
            var contribution = new ShareholderContribution
            {
                ShareholderId = req.ShareholderId,
                ProjectId = req.ProjectId,
                TransactionId = cashTx.Id,
                Amount = req.Amount,
                ContributionDate = req.ContributionDate,
                Description = descriptionText,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShareholderContributions.Add(contribution);
            await _context.SaveChangesAsync(cancellationToken);

            // ===== Cascading Allocation Logic =====
            // Iterate installments in chronological order, clearing outstanding balances first
            decimal remainingPayment = req.Amount;

            foreach (var installment in allInstallments)
            {
                if (remainingPayment <= 0) break;

                decimal required = installment.AmountPerShare * shareholder.NumberOfShares;
                decimal alreadyPaid = paidByInstallment.GetValueOrDefault(installment.Id, 0m);
                decimal outstanding = required - alreadyPaid;

                if (outstanding <= 0) continue; // fully paid, skip

                // Only start allocating once we reach installments that still have a balance
                decimal allocate = Math.Min(remainingPayment, outstanding);

                var allocation = new ShareholderPaymentAllocation
                {
                    ShareholderContributionId = contribution.Id,
                    ProjectInstallmentId = installment.Id,
                    AmountAllocated = allocate,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ShareholderPaymentAllocations.Add(allocation);
                remainingPayment -= allocate;

                // Once we've reached the target installment, stop cascading further
                if (installment.Id == req.ProjectInstallmentId)
                {
                    break;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("RecordContribution", "Shareholder", shareholder.Id.ToString(), null,
                new { contribution.Amount, contribution.ProjectId, cashTx.TransactionNumber, req.ProjectInstallmentId }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);

            var dto = new ShareholderContributionDto(
                contribution.Id,
                shareholder.Id,
                shareholder.Name,
                req.ProjectId,
                project?.Name,
                req.ProjectInstallmentId,
                targetInstallment.Name,
                cashTx.Id,
                cashTx.TransactionNumber,
                contribution.Amount,
                contribution.ContributionDate,
                contribution.Description,
                contribution.CreatedByUserId,
                user?.FullName ?? "",
                contribution.CreatedAt
            );

            return ApiResponse<ShareholderContributionDto>.SuccessResult(dto, "تم تسجيل الدفعة بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

// =============================================
// Delete Shareholder Command
// =============================================

public record DeleteShareholderCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteShareholderCommandHandler : IRequestHandler<DeleteShareholderCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteShareholderCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteShareholderCommand request, CancellationToken cancellationToken)
    {
        var shareholder = await _context.Shareholders
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shareholder == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        // Delete payment allocations
        var contributions = await _context.ShareholderContributions
            .Where(c => c.ShareholderId == request.Id)
            .ToListAsync(cancellationToken);

        var contribIds = contributions.Select(c => c.Id).ToList();
        var allocations = await _context.ShareholderPaymentAllocations
            .Where(a => contribIds.Contains(a.ShareholderContributionId))
            .ToListAsync(cancellationToken);

        _context.ShareholderPaymentAllocations.RemoveRange(allocations);
        _context.ShareholderContributions.RemoveRange(contributions);
        _context.Shareholders.Remove(shareholder);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("Delete", "Shareholder", shareholder.Id.ToString(), new { shareholder.Code, shareholder.Name }, null, cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف المساهم بنجاح");
    }
}

// =============================================
// Update & Delete Shareholder Contribution Commands
// =============================================

public record UpdateShareholderContributionCommand(int ContributionId, UpdateShareholderContributionRequest Request) : IRequest<ApiResponse<ShareholderContributionDto>>;

public class UpdateShareholderContributionCommandHandler : IRequestHandler<UpdateShareholderContributionCommand, ApiResponse<ShareholderContributionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateShareholderContributionCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderContributionDto>> Handle(UpdateShareholderContributionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        if (req.Amount <= 0)
        {
            return ApiResponse<ShareholderContributionDto>.FailureResult("مبلغ الدفع يجب أن يكون أكبر من صفر");
        }

        var contribution = await _context.ShareholderContributions
            .Include(c => c.Shareholder)
            .Include(c => c.Project)
            .Include(c => c.Transaction)
            .FirstOrDefaultAsync(c => c.Id == request.ContributionId, cancellationToken);

        if (contribution == null)
        {
            throw new NotFoundException("الدفعة غير موجودة");
        }

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            string descText = !string.IsNullOrWhiteSpace(req.Description)
                ? req.Description
                : $"مساهمة من المساهم: {contribution.Shareholder?.Name}";

            // Update Contribution
            contribution.Amount = req.Amount;
            contribution.ContributionDate = req.ContributionDate;
            contribution.Description = descText;

            // Update Cash Transaction if linked
            if (contribution.Transaction != null)
            {
                contribution.Transaction.Amount = req.Amount;
                contribution.Transaction.TransactionDate = req.ContributionDate;
                contribution.Transaction.Description = descText;
                if (!string.IsNullOrWhiteSpace(req.ReferenceNumber)) contribution.Transaction.ReferenceNumber = req.ReferenceNumber;
                if (!string.IsNullOrWhiteSpace(req.Notes)) contribution.Transaction.Notes = req.Notes;
            }

            // Remove old allocations and recalculate cascading allocations
            var oldAllocations = await _context.ShareholderPaymentAllocations
                .Where(a => a.ShareholderContributionId == contribution.Id)
                .ToListAsync(cancellationToken);

            _context.ShareholderPaymentAllocations.RemoveRange(oldAllocations);
            await _context.SaveChangesAsync(cancellationToken);

            if (contribution.ProjectId.HasValue)
            {
                int projectId = contribution.ProjectId.Value;
                var allInstallments = await _context.ProjectInstallments
                    .Where(i => i.ProjectId == projectId && i.IsActive)
                    .OrderBy(i => i.EndDate)
                    .ThenBy(i => i.Id)
                    .ToListAsync(cancellationToken);

                var existingAllocations = await _context.ShareholderPaymentAllocations
                    .Include(a => a.ShareholderContribution)
                    .Where(a => a.ShareholderContribution.ShareholderId == contribution.ShareholderId
                             && a.ShareholderContribution.ProjectId == projectId)
                    .GroupBy(a => a.ProjectInstallmentId)
                    .Select(g => new { InstallmentId = g.Key, TotalPaid = g.Sum(a => a.AmountAllocated) })
                    .ToListAsync(cancellationToken);

                var paidByInstallment = existingAllocations.ToDictionary(x => x.InstallmentId, x => x.TotalPaid);
                decimal remainingPayment = req.Amount;

                foreach (var installment in allInstallments)
                {
                    if (remainingPayment <= 0) break;

                    decimal required = installment.AmountPerShare * (contribution.Shareholder?.NumberOfShares ?? 1m);
                    decimal alreadyPaid = paidByInstallment.GetValueOrDefault(installment.Id, 0m);
                    decimal outstanding = required - alreadyPaid;

                    if (outstanding <= 0) continue;

                    decimal allocate = Math.Min(remainingPayment, outstanding);
                    _context.ShareholderPaymentAllocations.Add(new ShareholderPaymentAllocation
                    {
                        ShareholderContributionId = contribution.Id,
                        ProjectInstallmentId = installment.Id,
                        AmountAllocated = allocate,
                        CreatedAt = DateTime.UtcNow
                    });
                    remainingPayment -= allocate;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            await _auditService.LogAsync("UpdateContribution", "Shareholder", contribution.ShareholderId.ToString(), null, new { contribution.Id, contribution.Amount }, cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { contribution.CreatedByUserId }, cancellationToken);
            var dto = new ShareholderContributionDto(
                contribution.Id,
                contribution.ShareholderId,
                contribution.Shareholder?.Name ?? "",
                contribution.ProjectId ?? 0,
                contribution.Project?.Name,
                0,
                "",
                contribution.TransactionId,
                contribution.Transaction?.TransactionNumber,
                contribution.Amount,
                contribution.ContributionDate,
                contribution.Description,
                contribution.CreatedByUserId,
                user?.FullName ?? "",
                contribution.CreatedAt
            );

            return ApiResponse<ShareholderContributionDto>.SuccessResult(dto, "تم تعديل الدفعة بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record DeleteShareholderContributionCommand(int ContributionId) : IRequest<ApiResponse<bool>>;

public class DeleteShareholderContributionCommandHandler : IRequestHandler<DeleteShareholderContributionCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteShareholderContributionCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteShareholderContributionCommand request, CancellationToken cancellationToken)
    {
        var contribution = await _context.ShareholderContributions
            .Include(c => c.Transaction)
            .FirstOrDefaultAsync(c => c.Id == request.ContributionId, cancellationToken);

        if (contribution == null)
        {
            throw new NotFoundException("الدفعة غير موجودة");
        }

        var allocations = await _context.ShareholderPaymentAllocations
            .Where(a => a.ShareholderContributionId == contribution.Id)
            .ToListAsync(cancellationToken);

        _context.ShareholderPaymentAllocations.RemoveRange(allocations);
        if (contribution.Transaction != null)
        {
            _context.CashTransactions.Remove(contribution.Transaction);
        }
        _context.ShareholderContributions.Remove(contribution);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("DeleteContribution", "Shareholder", contribution.ShareholderId.ToString(), new { contribution.Id, contribution.Amount }, null, cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف الدفعة بنجاح");
    }
}
