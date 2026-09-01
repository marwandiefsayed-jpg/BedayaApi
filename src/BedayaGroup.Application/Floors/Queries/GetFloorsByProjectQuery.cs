using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Floors.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Floors.Queries;

public record GetFloorsByProjectQuery(int ProjectId) : IRequest<ApiResponse<List<FloorDto>>>;

public class GetFloorsByProjectQueryHandler : IRequestHandler<GetFloorsByProjectQuery, ApiResponse<List<FloorDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFloorsByProjectQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<FloorDto>>> Handle(GetFloorsByProjectQuery request, CancellationToken cancellationToken)
    {
        var floors = await _context.Floors
            .Where(f => f.ProjectId == request.ProjectId)
            .OrderBy(f => f.FloorNumber)
            .Select(f => new FloorDto(
                f.Id,
                f.ProjectId,
                f.FloorNumber,
                f.Name,
                f.Description,
                f.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<FloorDto>>.SuccessResult(floors);
    }
}
