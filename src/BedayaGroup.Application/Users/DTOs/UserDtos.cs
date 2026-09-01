namespace BedayaGroup.Application.Users.DTOs;

public record CreateUserRequest(
    string FullName,
    string Username,
    string Password,
    string? Phone,
    int RoleId
);

public record UpdateUserRequest(
    string FullName,
    string? Phone,
    int RoleId,
    bool IsActive
);

public record UserRoleDto(
    int Id,
    string Name,
    string ArabicName,
    string? Description
);
