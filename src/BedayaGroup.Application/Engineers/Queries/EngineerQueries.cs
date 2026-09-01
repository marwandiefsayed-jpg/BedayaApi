using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Engineers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Engineers.Queries;

public record GetEngineersQuery(int PageIndex = 1, int PageSize = 10, string? Search = null) : IRequest<ApiResponse<PaginatedList<EngineerDto>>>;

public class GetEngineersQueryHandler : IRequestHandler<GetEngineersQuery, ApiResponse<PaginatedList<EngineerDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEngineersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<EngineerDto>>> Handle(GetEngineersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Engineers.Include(e => e.ProjectEngineers).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(e => e.FullName.Contains(request.Search) || e.Code.Contains(request.Search) || (e.Phone != null && e.Phone.Contains(request.Search)));
        }

        var projectedQuery = query.OrderByDescending(e => e.CreatedAt)
            .Select(e => new EngineerDto(
                e.Id,
                e.Code,
                e.FullName,
                e.Phone,
                e.Email,
                e.Specialization,
                e.Notes,
                e.IsActive,
                e.CreatedAt,
                e.ProjectEngineers.Count
            ));

        var result = await PaginatedList<EngineerDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<EngineerDto>>.SuccessResult(result);
    }
}

public record GetProjectEngineersQuery(int ProjectId) : IRequest<ApiResponse<List<ProjectEngineerDto>>>;

public class GetProjectEngineersQueryHandler : IRequestHandler<GetProjectEngineersQuery, ApiResponse<List<ProjectEngineerDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectEngineersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ProjectEngineerDto>>> Handle(GetProjectEngineersQuery request, CancellationToken cancellationToken)
    {
        var peList = await _context.ProjectEngineers
            .Include(pe => pe.Project)
            .Include(pe => pe.Engineer)
            .Where(pe => pe.ProjectId == request.ProjectId)
            .AsNoTracking()
            .Select(pe => new ProjectEngineerDto(
                pe.Id,
                pe.ProjectId,
                pe.Project.Name,
                pe.EngineerId,
                pe.Engineer.FullName,
                pe.Engineer.Code,
                pe.Role,
                pe.StartDate,
                pe.EndDate,
                pe.Notes
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ProjectEngineerDto>>.SuccessResult(peList);
    }
}
