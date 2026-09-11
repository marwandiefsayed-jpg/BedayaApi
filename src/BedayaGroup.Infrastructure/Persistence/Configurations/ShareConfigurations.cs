using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ShareConfiguration : IEntityTypeConfiguration<Share>
{
    public void Configure(EntityTypeBuilder<Share> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(s => s.Shareholders)
            .WithOne(sh => sh.Share!)
            .HasForeignKey(sh => sh.ShareId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProjectInstallmentConfiguration : IEntityTypeConfiguration<ProjectInstallment>
{
    public void Configure(EntityTypeBuilder<ProjectInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.AmountPerShare)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShareholderPaymentAllocationConfiguration : IEntityTypeConfiguration<ShareholderPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<ShareholderPaymentAllocation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AmountAllocated)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(a => a.ShareholderContribution)
            .WithMany()
            .HasForeignKey(a => a.ShareholderContributionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ProjectInstallment)
            .WithMany()
            .HasForeignKey(a => a.ProjectInstallmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShareholderInstallmentPenaltyConfiguration : IEntityTypeConfiguration<ShareholderInstallmentPenalty>
{
    public void Configure(EntityTypeBuilder<ShareholderInstallmentPenalty> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PenaltyAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasOne(p => p.Shareholder)
            .WithMany()
            .HasForeignKey(p => p.ShareholderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProjectInstallment)
            .WithMany()
            .HasForeignKey(p => p.ProjectInstallmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

