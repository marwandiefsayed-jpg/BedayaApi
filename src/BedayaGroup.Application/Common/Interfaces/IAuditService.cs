namespace BedayaGroup.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string entityId, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
}
