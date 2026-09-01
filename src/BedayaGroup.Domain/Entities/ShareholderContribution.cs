using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class ShareholderContribution : BaseEntity
{
    public int ShareholderId { get; set; }
    public int? ProjectId { get; set; }
    public int? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ContributionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }

    public Shareholder Shareholder { get; set; } = null!;
    public Project? Project { get; set; }
    public CashTransaction? Transaction { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
