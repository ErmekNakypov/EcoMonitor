using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class AirQualityStationConfiguration : IEntityTypeConfiguration<AirQualityStation>
{
    public void Configure(EntityTypeBuilder<AirQualityStation> builder)
    {
        builder.Property(s => s.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(s => s.Locality)
            .HasMaxLength(200);

        builder.Property(s => s.ProviderName)
            .HasMaxLength(200);

        builder.HasIndex(s => new { s.ExternalId, s.Source }).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}
