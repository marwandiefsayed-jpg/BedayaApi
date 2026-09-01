using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class ProjectEngineerConfiguration : IEntityTypeConfiguration<ProjectEngineer>
{
    public void Configure(EntityTypeBuilder<ProjectEngineer> builder)
    {
        builder.HasKey(pe => pe.Id);

        builder.HasIndex(pe => pe.ProjectId);
        builder.HasIndex(pe => pe.EngineerId);

        builder.HasOne(pe => pe.Project)
            .WithMany(p => p.ProjectEngineers)
            .HasForeignKey(pe => pe.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pe => pe.Engineer)
            .WithMany(e => e.ProjectEngineers)
            .HasForeignKey(pe => pe.EngineerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
