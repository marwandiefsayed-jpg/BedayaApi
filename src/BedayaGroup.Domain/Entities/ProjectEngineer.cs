using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class ProjectEngineer : BaseEntity
{
    public int ProjectId { get; set; }
    public int EngineerId { get; set; }
    public string? Role { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }

    public Project Project { get; set; } = null!;
    public Engineer Engineer { get; set; } = null!;
}
