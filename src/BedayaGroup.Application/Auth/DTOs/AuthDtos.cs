namespace BedayaGroup.Application.Auth.DTOs;

public record LoginRequest(string Username, string Password);

public record AuthResponse(
    int Id,
    string FullName,
    string Username,
    string Token,
    DateTime ExpiresAt
);

public record UserDto(
    int Id,
    string FullName,
    string Username,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
