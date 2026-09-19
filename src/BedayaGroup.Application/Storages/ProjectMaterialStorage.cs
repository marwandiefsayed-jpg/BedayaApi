using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Storages;

/// <summary>
/// Helper for the dedicated material warehouse (المخزن) that belongs to a project.
/// Every project owns exactly one <see cref="Storage"/> of type <see cref="StorageType.Project"/>.
/// </summary>
public static class ProjectMaterialStorage
{
    public const string NamePrefix = "مخزن مشروع: ";

    public static string BuildName(string projectName) => $"{NamePrefix}{projectName}";

    public static Task<Storage?> FindAsync(IApplicationDbContext context, int projectId, CancellationToken cancellationToken = default)
        => context.Storages.FirstOrDefaultAsync(s => s.Type == StorageType.Project && s.ProjectId == projectId, cancellationToken);

    public static async Task<Storage> GetOrCreateAsync(IApplicationDbContext context, Project project, CancellationToken cancellationToken = default)
    {
        var storage = await FindAsync(context, project.Id, cancellationToken);
        if (storage != null) return storage;

        storage = new Storage
        {
            Name = BuildName(project.Name),
            Type = StorageType.Project,
            ProjectId = project.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Storages.Add(storage);
        await context.SaveChangesAsync(cancellationToken);
        return storage;
    }
}
