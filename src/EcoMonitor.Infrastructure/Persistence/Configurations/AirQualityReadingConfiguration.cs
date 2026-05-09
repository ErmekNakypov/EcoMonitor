using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class AirQualityReadingConfiguration : IEntityTypeConfiguration<AirQualityReading>
{
    public void Configure(EntityTypeBuilder<AirQualityReading> builder)
    {
        builder.HasIndex(r => new { r.StationId, r.MeasuredAt });
        builder.HasIndex(r => r.MeasuredAt);
    }
}
