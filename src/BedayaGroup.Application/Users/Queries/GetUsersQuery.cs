using BedayaGroup.Application.Auth.DTOs;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Users.Queries;

public record GetUsersQuery(int PageIndex = 1, int PageSize = 10, string? Search = null) : IRequest<ApiResponse<PaginatedList<UserDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ApiResponse<PaginatedList<UserDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.Include(u => u.Role).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(u => u.FullName.Contains(request.Search) || u.Username.Contains(request.Search) || (u.Phone != null && u.Phone.Contains(request.Search)));
        }

        var projectedQuery = query.OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto(
                u.Id,
                u.FullName,
                u.Username,
                u.Phone,
                u.RoleId,
                u.Role.Name,
                u.Role.ArabicName,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt
            ));

        var result = await PaginatedList<UserDto>.CreateAsync(projectedQuery, request.PageIndex, request.PageSize, cancellationToken);
        return ApiResponse<PaginatedList<UserDto>>.SuccessResult(result);
    }
}
