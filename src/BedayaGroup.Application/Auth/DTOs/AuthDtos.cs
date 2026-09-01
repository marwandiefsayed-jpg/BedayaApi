namespace BedayaGroup.Application.Auth.DTOs;

public record LoginRequest(string Username, string Password);

public record AuthResponse(
    int Id,
    string FullName,
    string Username,
    string RoleName,
    int RoleId,
    string Token,
    DateTime ExpiresAt
);

public record UserDto(
    int Id,
    string FullName,
    string Username,
    string? Phone,
    int RoleId,
    string RoleName,
    string RoleArabicName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
