using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class TelegramUserSessionConfiguration : IEntityTypeConfiguration<TelegramUserSession>
{
    public void Configure(EntityTypeBuilder<TelegramUserSession> builder)
    {
        builder.HasIndex(s => s.TelegramUserId).IsUnique();

        builder.Property(s => s.FirstName).HasMaxLength(200);
        builder.Property(s => s.UserName).HasMaxLength(200);
        builder.Property(s => s.DraftDescription).HasMaxLength(1000);

        builder.Property(s => s.Language)
            .IsRequired()
            .HasMaxLength(8)
            .HasDefaultValue("ru");

        var listComparer = new ValueComparer<List<string>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            c => c == null ? 0 : c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList());

        // Photo file_ids are short opaque tokens. Comma-join is sufficient and keeps the
        // column readable in psql; commas don't appear in Telegram file_id values.
        builder.Property(s => s.DraftPhotoFileIds)
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(listComparer);
    }
}
