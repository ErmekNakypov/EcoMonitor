using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class DumpsiteAppealPhotoConfiguration : IEntityTypeConfiguration<DumpsiteAppealPhoto>
{
    public void Configure(EntityTypeBuilder<DumpsiteAppealPhoto> builder)
    {
        builder.Property(p => p.FilePath).IsRequired().HasMaxLength(500);
        builder.HasIndex(p => p.ReportId);
    }
}
