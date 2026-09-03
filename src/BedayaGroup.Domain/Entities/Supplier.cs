using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SupplierType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
