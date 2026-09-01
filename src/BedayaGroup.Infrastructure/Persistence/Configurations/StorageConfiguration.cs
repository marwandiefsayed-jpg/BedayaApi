using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BedayaGroup.Infrastructure.Persistence.Configurations;

public class StorageConfiguration : IEntityTypeConfiguration<Storage>
{
    public void Configure(EntityTypeBuilder<Storage> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Type)
            .HasConversion<int>();

        builder.HasOne(s => s.Project)
            .WithMany(p => p.Storages)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
