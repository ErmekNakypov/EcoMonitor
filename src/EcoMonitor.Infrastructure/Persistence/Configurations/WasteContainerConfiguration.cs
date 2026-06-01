using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class WasteContainerConfiguration : IEntityTypeConfiguration<WasteContainer>
{
    public void Configure(EntityTypeBuilder<WasteContainer> builder)
    {
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Status);

        builder.HasIndex(c => c.OsmId)
            .IsUnique()
            .HasFilter("osm_id IS NOT NULL");

        builder.HasOne<District>()
            .WithMany()
            .HasForeignKey(c => c.DistrictId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(c => c.DistrictId);
    }
}
