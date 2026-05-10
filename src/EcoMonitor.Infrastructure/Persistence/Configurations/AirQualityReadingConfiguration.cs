using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class AirQualityReadingConfiguration : IEntityTypeConfiguration<AirQualityReading>
{
    public void Configure(EntityTypeBuilder<AirQualityReading> builder)
    {
        builder.HasOne<AirQualityStation>()
            .WithMany()
            .HasForeignKey(r => r.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.StationId, r.MeasuredAt })
            .IsDescending(false, true);

        builder.HasIndex(r => r.MeasuredAt);
    }
}
