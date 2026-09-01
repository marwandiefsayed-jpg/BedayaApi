using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class Floor : BaseEntity
{
    public int ProjectId { get; set; }
    public int FloorNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
