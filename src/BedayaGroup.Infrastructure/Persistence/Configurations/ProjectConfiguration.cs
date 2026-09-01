using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Location)
            .HasMaxLength(300);

        builder.Property(p => p.Budget)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.HasIndex(p => p.ProjectOwnerId);

        builder.HasOne(p => p.ProjectOwner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.ProjectOwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
