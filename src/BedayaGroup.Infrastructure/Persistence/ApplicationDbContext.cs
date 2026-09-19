using System.Reflection;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BedayaGroup.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Share> Shares => Set<Share>();
    public DbSet<ProjectInstallment> ProjectInstallments => Set<ProjectInstallment>();
    public DbSet<ProjectInstallmentShareholder> ProjectInstallmentShareholders => Set<ProjectInstallmentShareholder>();
    public DbSet<ShareholderPaymentAllocation> ShareholderPaymentAllocations => Set<ShareholderPaymentAllocation>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashStorage> CashStorages => Set<CashStorage>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<Shareholder> Shareholders => Set<Shareholder>();
    public DbSet<ShareholderContribution> ShareholderContributions => Set<ShareholderContribution>();
    public DbSet<ShareholderInstallmentPenalty> ShareholderInstallmentPenalties => Set<ShareholderInstallmentPenalty>();
    public DbSet<Storage> Storages => Set<Storage>();
    public DbSet<StorageTransaction> StorageTransactions => Set<StorageTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
