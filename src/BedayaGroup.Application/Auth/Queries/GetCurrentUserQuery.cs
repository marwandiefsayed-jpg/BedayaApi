using BedayaGroup.Application.Auth.DTOs;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<ApiResponse<UserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<UserDto>.FailureResult("غير مصرح بالوصول");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        var dto = new UserDto(
            user.Id,
            user.FullName,
            user.Username,
            user.Phone,
            user.RoleId,
            user.Role.Name,
            user.Role.ArabicName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        );

        return ApiResponse<UserDto>.SuccessResult(dto);
    }
}
