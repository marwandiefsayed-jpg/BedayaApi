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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Engineer> Engineers => Set<Engineer>();
    public DbSet<ProjectEngineer> ProjectEngineers => Set<ProjectEngineer>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashStorage> CashStorages => Set<CashStorage>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<Shareholder> Shareholders => Set<Shareholder>();
    public DbSet<ShareholderContribution> ShareholderContributions => Set<ShareholderContribution>();
    public DbSet<Advance> Advances => Set<Advance>();
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
