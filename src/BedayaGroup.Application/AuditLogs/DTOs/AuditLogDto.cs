namespace BedayaGroup.Application.AuditLogs.DTOs;

public record AuditLogDto(
    long Id,
    int? UserId,
    string? UserName,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    DateTime CreatedAt,
    string? IpAddress
);
