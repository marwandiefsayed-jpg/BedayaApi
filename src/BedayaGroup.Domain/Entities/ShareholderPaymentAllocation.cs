using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class ShareholderPaymentAllocation : BaseEntity
{
    public int ShareholderContributionId { get; set; }
    public int ProjectInstallmentId { get; set; }
    public decimal AmountAllocated { get; set; }

    public ShareholderContribution ShareholderContribution { get; set; } = null!;
    public ProjectInstallment ProjectInstallment { get; set; } = null!;
}
