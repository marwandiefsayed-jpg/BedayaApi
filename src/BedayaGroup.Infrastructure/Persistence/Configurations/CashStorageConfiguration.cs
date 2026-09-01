using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class CashStorageConfiguration : IEntityTypeConfiguration<CashStorage>
{
    public void Configure(EntityTypeBuilder<CashStorage> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Type)
            .HasConversion<int>();

        builder.Property(c => c.OpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
