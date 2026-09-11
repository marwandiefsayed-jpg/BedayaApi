using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shares.Queries;

// =============================================
// Share Queries
// =============================================

public record GetSharesQuery : IRequest<ApiResponse<List<ShareDto>>>;

public class GetSharesQueryHandler : IRequestHandler<GetSharesQuery, ApiResponse<List<ShareDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSharesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<ShareDto>>> Handle(GetSharesQuery request, CancellationToken cancellationToken)
    {
        var shares = await _context.Shares
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ShareDto(s.Id, s.Name, s.StartDate, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ShareDto>>.SuccessResult(shares);
    }
}

// =============================================
// ProjectInstallment Queries
// =============================================

public record GetProjectInstallmentsQuery(int ProjectId) : IRequest<ApiResponse<List<ProjectInstallmentDto>>>;

public class GetProjectInstallmentsQueryHandler : IRequestHandler<GetProjectInstallmentsQuery, ApiResponse<List<ProjectInstallmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectInstallmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<ProjectInstallmentDto>>> Handle(GetProjectInstallmentsQuery request, CancellationToken cancellationToken)
    {
        var installments = await _context.ProjectInstallments
            .Include(i => i.Project)
            .Where(i => i.ProjectId == request.ProjectId && i.IsActive)
            .OrderBy(i => i.EndDate)
            .ThenBy(i => i.Id)
            .AsNoTracking()
            .Select(i => new ProjectInstallmentDto(i.Id, i.ProjectId, i.Project.Name, i.Name, i.StartDate, i.EndDate, i.AmountPerShare, i.IsActive, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ProjectInstallmentDto>>.SuccessResult(installments);
    }
}
