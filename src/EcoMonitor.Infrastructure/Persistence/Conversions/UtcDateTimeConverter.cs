using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EcoMonitor.Infrastructure.Persistence.Conversions;

// Npgsql rejects DateTime values with Kind != Utc when writing to
// `timestamp with time zone` columns. Apply these converters globally so any
// DateTime read from the DB is tagged Utc, and any DateTime written is forced
// to Utc:
//   - Utc          → passed through unchanged
//   - Local        → converted via ToUniversalTime (accounts for offset)
//   - Unspecified  → tagged Utc without shifting (HTML <input type="date">
//                    produces midnight-Unspecified; we treat the calendar
//                    date as authoritative rather than reinterpreting it in
//                    the server's local timezone)
//
// ValueConverter ctor takes Expression<Func<,>>, which cannot contain switch
// expressions — hence the nested ternaries below.
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc
                ? v
                : (v.Kind == DateTimeKind.Local
                    ? v.ToUniversalTime()
                    : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => !v.HasValue
                ? v
                : (v.Value.Kind == DateTimeKind.Utc
                    ? v
                    : (v.Value.Kind == DateTimeKind.Local
                        ? (DateTime?)v.Value.ToUniversalTime()
                        : (DateTime?)DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))),
            v => v.HasValue ? (DateTime?)DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
