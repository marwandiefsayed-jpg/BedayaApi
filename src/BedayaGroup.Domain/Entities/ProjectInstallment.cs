using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class ProjectInstallment : BaseEntity
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>تاريخ بداية الدفعة</summary>
    public DateTime StartDate { get; set; }
    /// <summary>تاريخ نهاية الدفعة / الموعد النهائي للسداد</summary>
    public DateTime EndDate { get; set; }
    public decimal AmountPerShare { get; set; }
    public bool IsActive { get; set; } = true;

    public Project Project { get; set; } = null!;
}
