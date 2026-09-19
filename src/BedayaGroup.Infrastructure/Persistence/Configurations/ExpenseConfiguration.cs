using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.ExpenseNumber)
            .IsUnique();

        builder.Property(e => e.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(18,3)");

        builder.Property(e => e.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.MaterialName)
            .HasMaxLength(200);

        builder.Property(e => e.Unit)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<int>();

        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.StorageId);
        builder.HasIndex(e => e.ExpenseDate);

        builder.HasOne(e => e.Project)
            .WithMany(p => p.Expenses)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Storage)
            .WithMany()
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();
    }
}
