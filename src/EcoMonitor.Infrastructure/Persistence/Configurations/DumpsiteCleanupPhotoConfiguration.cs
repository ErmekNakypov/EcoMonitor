using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class DumpsiteCleanupPhotoConfiguration : IEntityTypeConfiguration<DumpsiteCleanupPhoto>
{
    public void Configure(EntityTypeBuilder<DumpsiteCleanupPhoto> builder)
    {
        builder.Property(p => p.FilePath).IsRequired().HasMaxLength(500);
        builder.HasIndex(p => p.ReportId);
        builder.HasIndex(p => new { p.ReportId, p.Type });
    }
}
