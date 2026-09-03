using BedayaGroup.Domain.Common;
using BedayaGroup.Domain.Enums;

namespace BedayaGroup.Domain.Entities;

public class Project : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public bool IsActive { get; set; } = true;

    public ICollection<Shareholder> Shareholders { get; set; } = new List<Shareholder>();
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    public ICollection<ProjectEngineer> ProjectEngineers { get; set; } = new List<ProjectEngineer>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Advance> Advances { get; set; } = new List<Advance>();
    public ICollection<Storage> Storages { get; set; } = new List<Storage>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
