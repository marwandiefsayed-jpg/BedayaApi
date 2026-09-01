using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shareholders.Queries;

public record GetShareholdersQuery(int PageIndex = 1, int PageSize = 10, string? Search = null) : IRequest<ApiResponse<PaginatedList<ShareholderDto>>>;

public class GetShareholdersQueryHandler : IRequestHandler<GetShareholdersQuery, ApiResponse<PaginatedList<ShareholderDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetShareholdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<ShareholderDto>>> Handle(GetShareholdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Shareholders.Include(s => s.ShareholderContributions).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(s => s.Name.Contains(request.Search) || s.Code.Contains(request.Search) || (s.Phone != null && s.Phone.Contains(request.Search)));
        }

        var projectedQuery = query.OrderByDescending(s => s.CreatedAt)
            .Select(s => new ShareholderDto(
                s.Id,
                s.Code,
                s.Name,
                s.Phone,
                s.OwnershipPercentage,
                s.RequiredContribution,
                s.ShareholderContributions.Sum(c => c.Amount),
                s.RequiredContribution - s.ShareholderContributions.Sum(c => c.Amount),
                s.Notes,
                s.IsActive,
                s.CreatedAt
            ));

        var result = await PaginatedList<ShareholderDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<ShareholderDto>>.SuccessResult(result);
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
            .Include(s => s.ShareholderContributions)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (s == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        var contributed = s.ShareholderContributions.Sum(c => c.Amount);
        var remaining = s.RequiredContribution - contributed;

        var dto = new ShareholderDto(s.Id, s.Code, s.Name, s.Phone, s.OwnershipPercentage, s.RequiredContribution, contributed, remaining, s.Notes, s.IsActive, s.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto);
    }
}

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
        var s = await _context.Shareholders.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShareholderId, cancellationToken);
        if (s == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        var contributions = await _context.ShareholderContributions
            .Include(sc => sc.Project)
            .Include(sc => sc.Transaction)
            .Include(sc => sc.CreatedByUser)
            .Where(sc => sc.ShareholderId == request.ShareholderId)
            .OrderByDescending(sc => sc.ContributionDate)
            .Select(sc => new ShareholderContributionDto(
                sc.Id,
                sc.ShareholderId,
                s.Name,
                sc.ProjectId,
                sc.Project != null ? sc.Project.Name : null,
                sc.TransactionId,
                sc.Transaction != null ? sc.Transaction.TransactionNumber : null,
                sc.Amount,
                sc.ContributionDate,
                sc.Description,
                sc.CreatedByUserId,
                sc.CreatedByUser.FullName,
                sc.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var totalContributed = contributions.Sum(c => c.Amount);
        var remaining = s.RequiredContribution - totalContributed;

        var statement = new ShareholderStatementDto(
            s.Id,
            s.Code,
            s.Name,
            s.OwnershipPercentage,
            s.RequiredContribution,
            totalContributed,
            remaining,
            contributions
        );

        return ApiResponse<ShareholderStatementDto>.SuccessResult(statement);
    }
}
