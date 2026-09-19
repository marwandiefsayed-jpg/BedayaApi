using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shareholders.Queries;

// =============================================
// Shareholder Queries
// =============================================

public record GetShareholdersQuery(int PageIndex = 1, int PageSize = 10, string? Search = null, int? ProjectId = null) : IRequest<ApiResponse<PaginatedList<ShareholderDto>>>;

public class GetShareholdersQueryHandler : IRequestHandler<GetShareholdersQuery, ApiResponse<PaginatedList<ShareholderDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetShareholdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ShareholderDto>>> Handle(GetShareholdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Shareholders
            .Include(s => s.Project)
            .AsNoTracking().AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(s => s.Name.Contains(request.Search) || s.Code.Contains(request.Search) || (s.Phone != null && s.Phone.Contains(request.Search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(s => s.CreatedAt)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var shareholderIds = items.Select(s => s.Id).ToList();
        var projectIds = items.Where(s => s.ProjectId.HasValue).Select(s => s.ProjectId!.Value).Distinct().ToList();

        var installments = await _context.ProjectInstallments
            .Include(pi => pi.TargetShareholders)
            .Where(pi => projectIds.Contains(pi.ProjectId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var penalties = await _context.ShareholderInstallmentPenalties
            .Where(p => shareholderIds.Contains(p.ShareholderId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var contributions = await _context.ShareholderContributions
            .Where(c => shareholderIds.Contains(c.ShareholderId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = items.Select(s =>
        {
            var sInstallments = installments.Where(pi => pi.ProjectId == s.ProjectId &&
                (!pi.TargetShareholders.Any() || pi.TargetShareholders.Any(target => target.ShareholderId == s.Id))).ToList();
            var sPenaltiesSum = penalties.Where(p => p.ShareholderId == s.Id).Sum(p => p.PenaltyAmount);
            var baseExpected = sInstallments.Sum(pi => pi.AmountPerShare * s.NumberOfShares);
            var expected = baseExpected + sPenaltiesSum;
            var paid = contributions.Where(c => c.ShareholderId == s.Id).Sum(c => c.Amount);
            var remaining = Math.Max(0m, expected - paid);

            return new ShareholderDto(
                s.Id,
                s.Code,
                s.Name,
                s.Phone,
                s.NumberOfShares,
                s.ProjectId,
                s.Project != null ? s.Project.Name : null,
                s.Notes,
                s.IsActive,
                s.CreatedAt,
                expected,
                paid,
                remaining
            );
        }).ToList();

        var paginatedResult = new PaginatedList<ShareholderDto>(dtos, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PaginatedList<ShareholderDto>>.SuccessResult(paginatedResult);
    }
}

public record GetShareholderByIdQuery(int Id) : IRequest<ApiResponse<ShareholderDto>>;

public class GetShareholderByIdQueryHandler : IRequestHandler<GetShareholderByIdQuery, ApiResponse<ShareholderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetShareholderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ShareholderDto>> Handle(GetShareholderByIdQuery request, CancellationToken cancellationToken)
    {
        var s = await _context.Shareholders
            .Include(sh => sh.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(sh => sh.Id == request.Id, cancellationToken);

        if (s == null) throw new NotFoundException("المساهم غير موجود");

        var dto = new ShareholderDto(s.Id, s.Code, s.Name, s.Phone, s.NumberOfShares, s.ProjectId, s.Project?.Name, s.Notes, s.IsActive, s.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto);
    }
}

// =============================================
// Full Statement with Installment Breakdown
// =============================================

public record GetShareholderStatementQuery(int ShareholderId) : IRequest<ApiResponse<ShareholderStatementDto>>;

public class GetShareholderStatementQueryHandler : IRequestHandler<GetShareholderStatementQuery, ApiResponse<ShareholderStatementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetShareholderStatementQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ShareholderStatementDto>> Handle(GetShareholderStatementQuery request, CancellationToken cancellationToken)
    {
        var shareholder = await _context.Shareholders
            .Include(s => s.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShareholderId, cancellationToken);

        if (shareholder == null) throw new NotFoundException("المساهم غير موجود");

        // Load installments for the shareholder's project
        var installments = shareholder.ProjectId.HasValue
            ? await _context.ProjectInstallments
                .Include(i => i.Project)
                .Where(i => i.ProjectId == shareholder.ProjectId.Value && i.IsActive &&
                    (!i.TargetShareholders.Any() || i.TargetShareholders.Any(target => target.ShareholderId == shareholder.Id)))
                .OrderBy(i => i.EndDate)
                .ThenBy(i => i.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : new List<Domain.Entities.ProjectInstallment>();

        // Load all allocations for this shareholder
        var allocations = await _context.ShareholderPaymentAllocations
            .Include(a => a.ShareholderContribution)
            .Where(a => a.ShareholderContribution.ShareholderId == request.ShareholderId)
            .GroupBy(a => a.ProjectInstallmentId)
            .Select(g => new { InstallmentId = g.Key, TotalPaid = g.Sum(a => a.AmountAllocated) })
            .ToListAsync(cancellationToken);

        var paidByInstallment = allocations.ToDictionary(x => x.InstallmentId, x => x.TotalPaid);

        // Load penalties for this shareholder (keyed by installment id)
        var penaltiesRaw = await _context.ShareholderInstallmentPenalties
            .Where(p => p.ShareholderId == request.ShareholderId)
            .GroupBy(p => p.ProjectInstallmentId)
            .Select(g => new { InstallmentId = g.Key, TotalPenalty = g.Sum(p => p.PenaltyAmount) })
            .ToListAsync(cancellationToken);

        var penaltyByInstallment = penaltiesRaw.ToDictionary(x => x.InstallmentId, x => x.TotalPenalty);

        // Build installment summary list
        var installmentSummaries = installments.Select(i =>
        {
            var baseRequired = i.AmountPerShare * shareholder.NumberOfShares;
            var penalty = penaltyByInstallment.GetValueOrDefault(i.Id, 0m);
            var required = baseRequired + penalty;
            var paid = paidByInstallment.GetValueOrDefault(i.Id, 0m);
            var remaining = required - paid;
            var status = paid <= 0
                ? InstallmentPaymentStatus.Unpaid
                : paid >= required
                    ? InstallmentPaymentStatus.FullyPaid
                    : InstallmentPaymentStatus.PartiallyPaid;

            return new InstallmentSummaryDto(i.Id, i.Name, i.StartDate, i.EndDate, i.AmountPerShare, penalty, required, paid, remaining, status);
        }).ToList();

        // Load contribution details
        var contributions = await _context.ShareholderContributions
            .Include(sc => sc.Project)
            .Include(sc => sc.Transaction)
            .Include(sc => sc.CreatedByUser)
            .Where(sc => sc.ShareholderId == request.ShareholderId)
            .OrderByDescending(sc => sc.ContributionDate)
            .ToListAsync(cancellationToken);

        // For each contribution, find which installment it was primarily targeting
        var contributionAllocations = await _context.ShareholderPaymentAllocations
            .Where(a => a.ShareholderContribution.ShareholderId == request.ShareholderId)
            .Select(a => new { a.ShareholderContributionId, a.ProjectInstallmentId, a.ProjectInstallment.Name, a.AmountAllocated })
            .ToListAsync(cancellationToken);

        var contribAllocationLookup = contributionAllocations
            .GroupBy(a => a.ShareholderContributionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AmountAllocated).First()); // primary allocation

        var contributionDtos = contributions.Select(sc =>
        {
            var primaryAlloc = contribAllocationLookup.GetValueOrDefault(sc.Id);
            return new ShareholderContributionDto(
                sc.Id,
                sc.ShareholderId,
                shareholder.Name,
                sc.ProjectId ?? 0,
                sc.Project?.Name,
                primaryAlloc?.ProjectInstallmentId ?? 0,
                primaryAlloc?.Name ?? "",
                sc.TransactionId,
                sc.Transaction?.TransactionNumber,
                sc.Amount,
                sc.ContributionDate,
                sc.Description,
                sc.CreatedByUserId,
                sc.CreatedByUser?.FullName ?? "",
                sc.CreatedAt
            );
        }).ToList();

        var totalExpected = installmentSummaries.Sum(i => i.RequiredAmount);
        var totalPaid = installmentSummaries.Sum(i => i.PaidAmount);
        var totalRemaining = totalExpected - totalPaid;

        var statement = new ShareholderStatementDto(
            shareholder.Id,
            shareholder.Code,
            shareholder.Name,
            shareholder.NumberOfShares,
            shareholder.ProjectId,
            shareholder.Project?.Name,
            totalExpected,
            totalPaid,
            totalRemaining,
            installmentSummaries,
            contributionDtos
        );

        return ApiResponse<ShareholderStatementDto>.SuccessResult(statement);
    }
}

// =============================================
// Shareholder Contributions Query
// =============================================

public record GetShareholderContributionsQuery(int ShareholderId, int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PaginatedList<ShareholderContributionDto>>>;

public class GetShareholderContributionsQueryHandler : IRequestHandler<GetShareholderContributionsQuery, ApiResponse<PaginatedList<ShareholderContributionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetShareholderContributionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ShareholderContributionDto>>> Handle(GetShareholderContributionsQuery request, CancellationToken cancellationToken)
    {
        var shareholder = await _context.Shareholders
            .FirstOrDefaultAsync(s => s.Id == request.ShareholderId, cancellationToken);

        if (shareholder == null) throw new NotFoundException("المساهم غير موجود");

        var query = _context.ShareholderContributions
            .Include(sc => sc.Project)
            .Include(sc => sc.Transaction)
            .Include(sc => sc.CreatedByUser)
            .Where(sc => sc.ShareholderId == request.ShareholderId)
            .OrderByDescending(sc => sc.ContributionDate)
            .AsNoTracking();

        var projected = query.Select(sc => new ShareholderContributionDto(
            sc.Id,
            sc.ShareholderId,
            shareholder.Name,
            sc.ProjectId ?? 0,
            sc.Project != null ? sc.Project.Name : null,
            0,
            "",
            sc.TransactionId,
            sc.Transaction != null ? sc.Transaction.TransactionNumber : null,
            sc.Amount,
            sc.ContributionDate,
            sc.Description,
            sc.CreatedByUserId,
            sc.CreatedByUser != null ? sc.CreatedByUser.FullName : "",
            sc.CreatedAt
        ));

        var result = await PaginatedList<ShareholderContributionDto>.CreateAsync(projected, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<ShareholderContributionDto>>.SuccessResult(result);
    }
}
