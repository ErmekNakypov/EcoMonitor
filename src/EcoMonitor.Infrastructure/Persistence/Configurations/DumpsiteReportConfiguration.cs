using System.Text.Json;
using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class DumpsiteReportConfiguration : IEntityTypeConfiguration<DumpsiteReport>
{
    public void Configure(EntityTypeBuilder<DumpsiteReport> builder)
    {
        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.ResolutionNotes)
            .HasMaxLength(1000);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList());

        builder.Property(r => r.PhotoPaths)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(r => r.Source)
            .HasDefaultValue(EcoMonitor.Domain.Enums.ReportSource.Web);

        builder.Property(r => r.TelegramUserName)
            .HasMaxLength(200);

        builder.Property(r => r.InspectorObservations).HasMaxLength(1000);
        builder.Property(r => r.CleanupNotes).HasMaxLength(1000);
        builder.Property(r => r.AutoTriageReason).HasMaxLength(500);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ReporterId);
        builder.HasIndex(r => r.AssignedInspectorId);
        builder.HasIndex(r => r.TelegramUserId);
        builder.HasIndex(r => r.CleanupCrewId);
    }
}
