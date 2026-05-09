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
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 2) return "yesterday";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} weeks ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} months ago";
        return $"{(int)(diff.TotalDays / 365)} years ago";
    }
}
