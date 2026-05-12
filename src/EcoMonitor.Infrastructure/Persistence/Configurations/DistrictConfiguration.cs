using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(32);
        builder.Property(d => d.NameRu).IsRequired().HasMaxLength(120);
        builder.Property(d => d.NameEn).IsRequired().HasMaxLength(120);
        builder.Property(d => d.NameKy).IsRequired().HasMaxLength(120);
        builder.Property(d => d.ColorHex).IsRequired().HasMaxLength(16);
        builder.HasIndex(d => d.Code).IsUnique();

        builder.HasMany(d => d.Boundary)
            .WithOne(b => b.District)
            .HasForeignKey(b => b.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DistrictBoundaryPointConfiguration : IEntityTypeConfiguration<DistrictBoundaryPoint>
{
    public void Configure(EntityTypeBuilder<DistrictBoundaryPoint> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.DistrictId, b.SequenceNumber });
    }
}
