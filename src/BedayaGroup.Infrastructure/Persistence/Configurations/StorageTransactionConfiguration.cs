using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class StorageTransactionConfiguration : IEntityTypeConfiguration<StorageTransaction>
{
    public void Configure(EntityTypeBuilder<StorageTransaction> builder)
    {
        builder.HasKey(st => st.Id);

        builder.Property(st => st.MaterialName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(st => st.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.Quantity)
            .HasColumnType("decimal(18,3)");

        builder.Property(st => st.Type)
            .HasConversion<int>();

        builder.HasIndex(st => st.StorageId);
        builder.HasIndex(st => st.ProjectId);
        builder.HasIndex(st => st.TransactionDate);

        builder.HasOne(st => st.Storage)
            .WithMany(s => s.StorageTransactions)
            .HasForeignKey(st => st.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.Project)
            .WithMany()
            .HasForeignKey(st => st.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.CreatedByUser)
            .WithMany()
            .HasForeignKey(st => st.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
