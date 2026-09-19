using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

/// <summary>Restricts a manual installment to the explicitly selected shareholder.</summary>
public class ProjectInstallmentShareholder : BaseEntity
{
    public int ProjectInstallmentId { get; set; }
    public int ShareholderId { get; set; }

    public ProjectInstallment ProjectInstallment { get; set; } = null!;
    public Shareholder Shareholder { get; set; } = null!;
}
