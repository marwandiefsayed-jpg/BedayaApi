using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ShareholderContributionConfiguration : IEntityTypeConfiguration<ShareholderContribution>
{
    public void Configure(EntityTypeBuilder<ShareholderContribution> builder)
    {
        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Amount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(sc => sc.ShareholderId);
        builder.HasIndex(sc => sc.ProjectId);

        builder.HasOne(sc => sc.Shareholder)
            .WithMany(s => s.ShareholderContributions)
            .HasForeignKey(sc => sc.ShareholderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sc => sc.Project)
            .WithMany()
            .HasForeignKey(sc => sc.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sc => sc.Transaction)
            .WithMany()
            .HasForeignKey(sc => sc.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sc => sc.CreatedByUser)
            .WithMany()
            .HasForeignKey(sc => sc.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
