using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class Expense : BaseEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int SupplierId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MaterialName { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public int CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Due;

    public Project Project { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
