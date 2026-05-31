using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class IotDeviceConfiguration : IEntityTypeConfiguration<IotDevice>
{
    public void Configure(EntityTypeBuilder<IotDevice> builder)
    {
        builder.Property(d => d.DeviceId).IsRequired().HasMaxLength(64);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.TokenHash).IsRequired().HasMaxLength(128);

        builder.HasIndex(d => d.DeviceId).IsUnique();
        builder.HasIndex(d => d.Status);

        builder.HasOne<WasteContainer>()
            .WithMany()
            .HasForeignKey(d => d.ContainerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.ContainerId);
    }
}
