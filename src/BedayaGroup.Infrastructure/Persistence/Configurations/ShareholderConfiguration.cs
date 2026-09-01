using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ShareholderConfiguration : IEntityTypeConfiguration<Shareholder>
{
    public void Configure(EntityTypeBuilder<Shareholder> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.OwnershipPercentage)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.RequiredContribution)
            .HasColumnType("decimal(18,2)");
    }
}
