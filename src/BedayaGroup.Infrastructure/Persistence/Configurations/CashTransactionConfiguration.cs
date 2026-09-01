using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.TransactionNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ct => ct.TransactionNumber)
            .IsUnique();

        builder.Property(ct => ct.Type)
            .HasConversion<int>();

        builder.Property(ct => ct.Amount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(ct => ct.CashStorageId);
        builder.HasIndex(ct => ct.ProjectId);
        builder.HasIndex(ct => ct.ExpenseId);
        builder.HasIndex(ct => ct.TransactionDate);

        builder.HasOne(ct => ct.CashStorage)
            .WithMany(cs => cs.CashTransactions)
            .HasForeignKey(ct => ct.CashStorageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ct => ct.Project)
            .WithMany(p => p.CashTransactions)
            .HasForeignKey(ct => ct.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ct => ct.Expense)
            .WithMany(e => e.CashTransactions)
            .HasForeignKey(ct => ct.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ct => ct.Advance)
            .WithMany(a => a.CashTransactions)
            .HasForeignKey(ct => ct.AdvanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ct => ct.CreatedByUser)
            .WithMany()
            .HasForeignKey(ct => ct.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ct => ct.RowVersion)
            .IsRowVersion();
    }
}
