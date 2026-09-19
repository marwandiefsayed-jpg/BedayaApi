using BedayaGroup.Domain.Common;

namespace BedayaGroup.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Shareholder> Shareholders { get; set; } = new List<Shareholder>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Storage> Storages { get; set; } = new List<Storage>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
