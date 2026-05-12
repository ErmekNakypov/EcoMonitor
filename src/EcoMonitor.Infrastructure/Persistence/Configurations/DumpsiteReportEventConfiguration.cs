using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class DumpsiteReportEventConfiguration : IEntityTypeConfiguration<DumpsiteReportEvent>
{
    public void Configure(EntityTypeBuilder<DumpsiteReportEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ActorRole).HasMaxLength(32).IsRequired();
        builder.Property(e => e.ActorDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasOne(e => e.Report)
            .WithMany(r => r.Events)
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ReportId, e.OccurredAt });
    }
}
