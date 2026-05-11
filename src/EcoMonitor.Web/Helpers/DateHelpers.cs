using System.Globalization;

namespace EcoMonitor.Web.Helpers;

public static class DateHelpers
{
    public static string FormatShort(DateTime utc) =>
        utc.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    public static string FormatLong(DateTime utc) =>
        utc.ToLocalTime().ToString("dddd, d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    public static string FormatRelative(DateTime utc)
    {
        var now = DateTime.UtcNow;
        var diff = now - utc;
        if (diff.TotalSeconds < 60) return "just now";

        var min = (int)diff.TotalMinutes;
        if (min < 60) return min == 1 ? "1 minute ago" : $"{min} minutes ago";

        var hr = (int)diff.TotalHours;
        if (hr < 24) return hr == 1 ? "1 hour ago" : $"{hr} hours ago";

        var d = (int)diff.TotalDays;
        if (d < 2) return "yesterday";
        if (d < 7) return $"{d} days ago";

        var w = d / 7;
        if (d < 30) return w == 1 ? "1 week ago" : $"{w} weeks ago";

        var m = d / 30;
        if (d < 365) return m == 1 ? "1 month ago" : $"{m} months ago";

        var y = d / 365;
        return y == 1 ? "1 year ago" : $"{y} years ago";
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

        if (date == today) return "Today";
        if (date == today.AddDays(-1)) return "Yesterday";
        if (date == today.AddDays(1)) return "Tomorrow";

        var diff = (today - date).Days;
        if (diff > 0 && diff <= 7)
        {
            return diff == 1 ? "1 day ago" : $"{diff} days ago";
        }

        return date.ToString("MMM d, yyyy", culture);
    }

    public static string FormatDate(DateTime? dateTime, CultureInfo? culture = null) =>
        dateTime.HasValue ? FormatDate(dateTime.Value, culture) : "—";
}
