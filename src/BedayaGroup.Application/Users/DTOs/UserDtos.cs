using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Application.Users.DTOs;

public record CreateUserRequest(
    string FullName,
    string Username,
    string Password,
    string? Phone,
    UserRole Role = UserRole.CompanyOwner
);

public record UpdateUserRequest(
    string FullName,
    string? Phone,
    UserRole Role,
    bool IsActive
);

public record UserRoleDto(
    int Id,
    string Name,
    string ArabicName,
    string? Description
);
