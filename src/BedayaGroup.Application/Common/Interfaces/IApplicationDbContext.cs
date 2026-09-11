using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BedayaGroup.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Share> Shares { get; }
    DbSet<ProjectInstallment> ProjectInstallments { get; }
    DbSet<ShareholderPaymentAllocation> ShareholderPaymentAllocations { get; }
    DbSet<Project> Projects { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Engineer> Engineers { get; }
    DbSet<ProjectEngineer> ProjectEngineers { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<CashStorage> CashStorages { get; }
    DbSet<CashTransaction> CashTransactions { get; }
    DbSet<Shareholder> Shareholders { get; }
    DbSet<ShareholderContribution> ShareholderContributions { get; }
    DbSet<ShareholderInstallmentPenalty> ShareholderInstallmentPenalties { get; }
    DbSet<Advance> Advances { get; }
    DbSet<Storage> Storages { get; }
    DbSet<StorageTransaction> StorageTransactions { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
