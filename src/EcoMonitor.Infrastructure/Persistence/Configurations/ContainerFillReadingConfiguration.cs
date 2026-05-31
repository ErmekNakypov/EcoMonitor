using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class ContainerFillReadingConfiguration : IEntityTypeConfiguration<ContainerFillReading>
{
    public void Configure(EntityTypeBuilder<ContainerFillReading> builder)
    {
        builder.HasOne<WasteContainer>()
            .WithMany()
            .HasForeignKey(r => r.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ContainerId, r.MeasuredAt })
            .IsDescending(false, true);

        builder.HasIndex(r => r.MeasuredAt);
    }
}
