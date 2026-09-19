using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class CashTransaction : BaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public CashTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public int CashStorageId { get; set; }
    public int? ProjectId { get; set; }
    public int? ExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public int CreatedByUserId { get; set; }
    public string? Notes { get; set; }

    public CashStorage CashStorage { get; set; } = null!;
    public Project? Project { get; set; }
    public Expense? Expense { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
