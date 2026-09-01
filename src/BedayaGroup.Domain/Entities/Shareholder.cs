using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class Shareholder : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public decimal RequiredContribution { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ShareholderContribution> ShareholderContributions { get; set; } = new List<ShareholderContribution>();
}
