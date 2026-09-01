using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class StorageTransaction : BaseEntity
{
    public int StorageId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public StorageTransactionType Type { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }

    public Storage Storage { get; set; } = null!;
    public Project? Project { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
