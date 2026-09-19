using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shares.Commands;

// =============================================
// Share Commands
// =============================================

public record CreateShareCommand(CreateShareRequest Request) : IRequest<ApiResponse<ShareDto>>;

public class CreateShareCommandHandler : IRequestHandler<CreateShareCommand, ApiResponse<ShareDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateShareCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ShareDto>> Handle(CreateShareCommand request, CancellationToken cancellationToken)
    {
        var share = new Share
        {
            Name = request.Request.Name,
            StartDate = request.Request.StartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShareDto>.SuccessResult(new ShareDto(share.Id, share.Name, share.StartDate, share.IsActive, share.CreatedAt), "تم إنشاء السهم بنجاح");
    }
}

public record UpdateShareCommand(int Id, UpdateShareRequest Request) : IRequest<ApiResponse<ShareDto>>;

public class UpdateShareCommandHandler : IRequestHandler<UpdateShareCommand, ApiResponse<ShareDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateShareCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ShareDto>> Handle(UpdateShareCommand request, CancellationToken cancellationToken)
    {
        var share = await _context.Shares.FindAsync(new object[] { request.Id }, cancellationToken);
        if (share == null) throw new NotFoundException("السهم غير موجود");

        share.Name = request.Request.Name;
        share.StartDate = request.Request.StartDate;
        share.IsActive = request.Request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<ShareDto>.SuccessResult(new ShareDto(share.Id, share.Name, share.StartDate, share.IsActive, share.CreatedAt), "تم تحديث السهم بنجاح");
    }
}

// =============================================
// ProjectInstallment Commands
// =============================================

public record CreateProjectInstallmentCommand(CreateProjectInstallmentRequest Request) : IRequest<ApiResponse<ProjectInstallmentDto>>;

public class CreateProjectInstallmentCommandHandler : IRequestHandler<CreateProjectInstallmentCommand, ApiResponse<ProjectInstallmentDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateProjectInstallmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ProjectInstallmentDto>> Handle(CreateProjectInstallmentCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var project = await _context.Projects.FindAsync(new object[] { req.ProjectId }, cancellationToken);
        if (project == null)
        {
            return ApiResponse<ProjectInstallmentDto>.FailureResult("المشروع المحدد غير موجود");
        }

        var installment = new ProjectInstallment
        {
            ProjectId = req.ProjectId,
            Name = req.Name,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            AmountPerShare = req.AmountPerShare,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectInstallments.Add(installment);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProjectInstallmentDto(installment.Id, installment.ProjectId, project.Name, installment.Name, installment.StartDate, installment.EndDate, installment.AmountPerShare, installment.IsActive, installment.CreatedAt);
        return ApiResponse<ProjectInstallmentDto>.SuccessResult(dto, "تم إنشاء الدفعة/القسط بنجاح");
    }
}

public record CreateManualInstallmentForShareholdersCommand(CreateManualInstallmentForShareholdersRequest Request) : IRequest<ApiResponse<ProjectInstallmentDto>>;

public class CreateManualInstallmentForShareholdersCommandHandler : IRequestHandler<CreateManualInstallmentForShareholdersCommand, ApiResponse<ProjectInstallmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateManualInstallmentForShareholdersCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<ProjectInstallmentDto>> Handle(CreateManualInstallmentForShareholdersCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        if (req.DirectAmount <= 0)
            return ApiResponse<ProjectInstallmentDto>.FailureResult("مبلغ الدفعة يجل أن يكون أكبر من صفر");

        if (req.ShareholderIds == null || !req.ShareholderIds.Any())
            return ApiResponse<ProjectInstallmentDto>.FailureResult("يرجى اختيار مساهم واحد على الأقل");

        var project = await _context.Projects.FindAsync(new object[] { req.ProjectId }, cancellationToken);
        if (project == null)
            return ApiResponse<ProjectInstallmentDto>.FailureResult("المشروع المحدد غير موجود");

        var shareholders = await _context.Shareholders
            .Where(s => req.ShareholderIds.Contains(s.Id) && s.ProjectId == req.ProjectId)
            .ToListAsync(cancellationToken);

        if (!shareholders.Any())
            return ApiResponse<ProjectInstallmentDto>.FailureResult("لم يتم العثور على مساهمين تابعين لهذا المشروع");

        decimal totalSelectedShares = shareholders.Sum(s => s.NumberOfShares);
        if (totalSelectedShares <= 0) totalSelectedShares = 1m;

        // AmountPerShare is calculated so that the selected shareholders pay exactly DirectAmount relative to their shares
        decimal amountPerShare = req.DirectAmount / totalSelectedShares;

        var installment = new ProjectInstallment
        {
            ProjectId = req.ProjectId,
            Name = req.Name,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            AmountPerShare = amountPerShare,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectInstallments.Add(installment);
        await _context.SaveChangesAsync(cancellationToken);

        _context.ProjectInstallmentShareholders.AddRange(shareholders.Select(shareholder => new ProjectInstallmentShareholder
        {
            ProjectInstallmentId = installment.Id,
            ShareholderId = shareholder.Id,
            CreatedAt = DateTime.UtcNow
        }));
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProjectInstallmentDto(installment.Id, installment.ProjectId, project.Name, installment.Name, installment.StartDate, installment.EndDate, installment.AmountPerShare, installment.IsActive, installment.CreatedAt);
        return ApiResponse<ProjectInstallmentDto>.SuccessResult(dto, "تم إضافة الدفعة بنجاح");
    }
}

public record DeleteProjectInstallmentCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteProjectInstallmentCommandHandler : IRequestHandler<DeleteProjectInstallmentCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteProjectInstallmentCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteProjectInstallmentCommand request, CancellationToken cancellationToken)
    {
        var installment = await _context.ProjectInstallments
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (installment == null) throw new NotFoundException("الدفعة غير موجودة");

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // Find all allocations for this installment
            var allocations = await _context.ShareholderPaymentAllocations
                .Include(a => a.ShareholderContribution)
                    .ThenInclude(c => c.Transaction)
                .Where(a => a.ProjectInstallmentId == request.Id)
                .ToListAsync(cancellationToken);

            var contribIds = allocations.Select(a => a.ShareholderContributionId).Distinct().ToList();

            if (contribIds.Any())
            {
                // Find all contributions affected by this installment
                var contributions = await _context.ShareholderContributions
                    .Include(c => c.Transaction)
                    .Where(c => contribIds.Contains(c.Id))
                    .ToListAsync(cancellationToken);

                // Find all allocations for these contributions to check multi-installment contributions
                var allAllocationsForContribs = await _context.ShareholderPaymentAllocations
                    .Where(a => contribIds.Contains(a.ShareholderContributionId))
                    .ToListAsync(cancellationToken);

                foreach (var contrib in contributions)
                {
                    var allocsForThisContrib = allAllocationsForContribs
                        .Where(a => a.ShareholderContributionId == contrib.Id)
                        .ToList();

                    var targetAlloc = allocsForThisContrib.FirstOrDefault(a => a.ProjectInstallmentId == request.Id);
                    decimal allocAmountToRemove = targetAlloc?.AmountAllocated ?? 0m;

                    var otherAllocs = allocsForThisContrib.Where(a => a.ProjectInstallmentId != request.Id).ToList();

                    if (!otherAllocs.Any() || contrib.Amount <= allocAmountToRemove)
                    {
                        // Entire contribution belonged to this deleted installment -> remove transaction & contribution
                        if (contrib.Transaction != null)
                        {
                            _context.CashTransactions.Remove(contrib.Transaction);
                        }
                        _context.ShareholderContributions.Remove(contrib);
                    }
                    else
                    {
                        // Partial allocation -> reduce contribution & transaction amount by the deleted installment's allocation
                        contrib.Amount -= allocAmountToRemove;
                        if (contrib.Transaction != null)
                        {
                            contrib.Transaction.Amount -= allocAmountToRemove;
                            if (contrib.Transaction.Amount <= 0)
                            {
                                _context.CashTransactions.Remove(contrib.Transaction);
                            }
                        }
                    }
                }

                // Remove all allocations linked to this installment
                _context.ShareholderPaymentAllocations.RemoveRange(allocations);
            }

            // Remove all penalties linked to this installment
            var penalties = await _context.ShareholderInstallmentPenalties
                .Where(p => p.ProjectInstallmentId == request.Id)
                .ToListAsync(cancellationToken);

            if (penalties.Any())
            {
                _context.ShareholderInstallmentPenalties.RemoveRange(penalties);
            }

            // Remove the installment itself
            _context.ProjectInstallments.Remove(installment);

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            await _auditService.LogAsync("DeleteProjectInstallment", "ProjectInstallment", installment.Id.ToString(), new { installment.Name, installment.ProjectId }, null, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true, "تم حذف الدفعة وتحديث الخزينة وحساب المساهمين بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}


public record UpdateProjectInstallmentCommand(int Id, UpdateProjectInstallmentRequest Request) : IRequest<ApiResponse<ProjectInstallmentDto>>;

public class UpdateProjectInstallmentCommandHandler : IRequestHandler<UpdateProjectInstallmentCommand, ApiResponse<ProjectInstallmentDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateProjectInstallmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ProjectInstallmentDto>> Handle(UpdateProjectInstallmentCommand request, CancellationToken cancellationToken)
    {
        var installment = await _context.ProjectInstallments
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (installment == null) throw new NotFoundException("القسط غير موجود");

        var req = request.Request;
        installment.Name = req.Name;
        installment.StartDate = req.StartDate;
        installment.EndDate = req.EndDate;
        installment.AmountPerShare = req.AmountPerShare;
        installment.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProjectInstallmentDto(installment.Id, installment.ProjectId, installment.Project.Name, installment.Name, installment.StartDate, installment.EndDate, installment.AmountPerShare, installment.IsActive, installment.CreatedAt);
        return ApiResponse<ProjectInstallmentDto>.SuccessResult(dto, "تم تحديث القسط بنجاح");
    }
}
