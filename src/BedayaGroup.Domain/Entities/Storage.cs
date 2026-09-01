using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class Storage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public StorageType Type { get; set; }
    public int? ProjectId { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Project? Project { get; set; }
    public ICollection<StorageTransaction> StorageTransactions { get; set; } = new List<StorageTransaction>();
}
