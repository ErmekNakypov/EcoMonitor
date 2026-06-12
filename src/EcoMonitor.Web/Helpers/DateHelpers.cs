using System.Globalization;

namespace EcoMonitor.Web.Helpers;

public enum RelativeTimeUnit
{
    JustNow,
    MinuteAgo,
    MinutesAgo,
    HourAgo,
    HoursAgo,
    Yesterday,
    DayAgo,
    DaysAgo,
    WeekAgo,
    WeeksAgo,
    MonthAgo,
    MonthsAgo,
    YearAgo,
    YearsAgo
}

// Subset used by FormatDate (calendar-date variant). DayAgo / DaysAgo are
// reused via RelativeResolver since the wording overlaps.
public enum CalendarDayMarker
{
    Today,
    Yesterday,
    Tomorrow
}

public static class DateHelpers
{
    // Host-supplied resolvers wired in Program.cs to an IStringLocalizer
    // over SharedResource. Each returns null when no resource hit, in
    // which case the helper falls back to the English literal below.
    //
    // RelativeResolver receives the bucket + the absolute count; the
    // resolver decides whether to format "{0}" into the value string
    // or to ignore the count for singular forms. {0} is the absolute
    // count for the *Ago plural buckets, ignored for the singletons.
    public static Func<RelativeTimeUnit, int, string?>? RelativeResolver { get; set; }
    public static Func<CalendarDayMarker, string?>? CalendarMarkerResolver { get; set; }

    public static string FormatShort(DateTime utc) =>
        utc.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    public static string FormatLong(DateTime utc) =>
        utc.ToLocalTime().ToString("dddd, d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    public static string FormatRelative(DateTime utc)
    {
        var now = DateTime.UtcNow;
        var diff = now - utc;
        if (diff.TotalSeconds < 60) return Resolve(RelativeTimeUnit.JustNow, 0, "just now");

        var min = (int)diff.TotalMinutes;
        if (min < 60) return min == 1
            ? Resolve(RelativeTimeUnit.MinuteAgo, 1, "1 minute ago")
            : Resolve(RelativeTimeUnit.MinutesAgo, min, $"{min} minutes ago");

        var hr = (int)diff.TotalHours;
        if (hr < 24) return hr == 1
            ? Resolve(RelativeTimeUnit.HourAgo, 1, "1 hour ago")
            : Resolve(RelativeTimeUnit.HoursAgo, hr, $"{hr} hours ago");

        var d = (int)diff.TotalDays;
        if (d < 2) return Resolve(RelativeTimeUnit.Yesterday, 1, "yesterday");
        if (d < 7) return Resolve(RelativeTimeUnit.DaysAgo, d, $"{d} days ago");

        var w = d / 7;
        if (d < 30) return w == 1
            ? Resolve(RelativeTimeUnit.WeekAgo, 1, "1 week ago")
            : Resolve(RelativeTimeUnit.WeeksAgo, w, $"{w} weeks ago");

        var m = d / 30;
        if (d < 365) return m == 1
            ? Resolve(RelativeTimeUnit.MonthAgo, 1, "1 month ago")
            : Resolve(RelativeTimeUnit.MonthsAgo, m, $"{m} months ago");

        var y = d / 365;
        return y == 1
            ? Resolve(RelativeTimeUnit.YearAgo, 1, "1 year ago")
            : Resolve(RelativeTimeUnit.YearsAgo, y, $"{y} years ago");
    }

    // For fields that represent a calendar date rather than a moment in time
    // (e.g. WasteContainer.InstalledAt sourced from <input type="date">).
    // FormatRelative would lie about these — "17 hours ago" for something
    // that happened "today" — because the value is midnight UTC.
    public static string FormatDate(DateTime dateTime, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var today = DateTime.UtcNow.Date;
        var date = dateTime.Date;

        if (date == today) return ResolveMarker(CalendarDayMarker.Today, "Today");
        if (date == today.AddDays(-1)) return ResolveMarker(CalendarDayMarker.Yesterday, "Yesterday");
        if (date == today.AddDays(1)) return ResolveMarker(CalendarDayMarker.Tomorrow, "Tomorrow");

        var diff = (today - date).Days;
        if (diff > 0 && diff <= 7)
        {
            return diff == 1
                ? Resolve(RelativeTimeUnit.DayAgo, 1, "1 day ago")
                : Resolve(RelativeTimeUnit.DaysAgo, diff, $"{diff} days ago");
        }

        return date.ToString("MMM d, yyyy", culture);
    }

    public static string FormatDate(DateTime? dateTime, CultureInfo? culture = null) =>
        dateTime.HasValue ? FormatDate(dateTime.Value, culture) : "—";

    private static string Resolve(RelativeTimeUnit unit, int count, string fallback)
    {
        var localized = RelativeResolver?.Invoke(unit, count);
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }

    private static string ResolveMarker(CalendarDayMarker marker, string fallback)
    {
        var localized = CalendarMarkerResolver?.Invoke(marker);
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
}
