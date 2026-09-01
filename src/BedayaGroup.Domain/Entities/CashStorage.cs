using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class CashStorage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CashStorageType Type { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? Location { get; set; }
    public int? ProjectId { get; set; }
    public bool IsActive { get; set; } = true;

    public Project? Project { get; set; }
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
