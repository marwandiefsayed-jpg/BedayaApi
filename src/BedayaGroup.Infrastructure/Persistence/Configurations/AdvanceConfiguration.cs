using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class AdvanceConfiguration : IEntityTypeConfiguration<Advance>
{
    public void Configure(EntityTypeBuilder<Advance> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdvanceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AdvanceNumber)
            .IsUnique();

        builder.Property(a => a.IssuedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.Status)
            .HasConversion<int>();

        builder.HasIndex(a => a.EngineerId);
        builder.HasIndex(a => a.ProjectId);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Engineer)
            .WithMany(e => e.Advances)
            .HasForeignKey(a => a.EngineerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Advances)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion)
            .IsRowVersion();
    }
}
