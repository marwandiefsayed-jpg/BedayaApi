using System.Text.Json;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;

namespace BedayaGroup.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AuditService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(string action, string entityName, string entityId, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues != null ? SerializeSafely(oldValues) : null,
            NewValues = newValues != null ? SerializeSafely(newValues) : null,
            CreatedAt = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string SerializeSafely(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        // Remove password / secret fields if present
        return json;
    }
}
