using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class Advance : BaseEntity
{
    public string AdvanceNumber { get; set; } = string.Empty;
    public int EngineerId { get; set; }
    public int ProjectId { get; set; }
    public decimal IssuedAmount { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public AdvanceStatus Status { get; set; } = AdvanceStatus.Open;
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }

    public Engineer Engineer { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
