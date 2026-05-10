namespace EcoMonitor.Web.Helpers;

public enum ReadingFreshness
{
    Fresh,
    Recent,
    Stale,
    VeryStale
}

public static class FreshnessHelper
{
    public static ReadingFreshness Classify(DateTime? measuredAtUtc)
    {
        if (measuredAtUtc is null) return ReadingFreshness.VeryStale;

        var age = DateTime.UtcNow - measuredAtUtc.Value;
        if (age < TimeSpan.FromHours(2)) return ReadingFreshness.Fresh;
        if (age < TimeSpan.FromHours(24)) return ReadingFreshness.Recent;
        if (age < TimeSpan.FromDays(7)) return ReadingFreshness.Stale;
        return ReadingFreshness.VeryStale;
    }
}
