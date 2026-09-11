using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class Share : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Shareholder> Shareholders { get; set; } = new List<Shareholder>();
}
