using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class Engineer : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialization { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProjectEngineer> ProjectEngineers { get; set; } = new List<ProjectEngineer>();
    public ICollection<Advance> Advances { get; set; } = new List<Advance>();
}
