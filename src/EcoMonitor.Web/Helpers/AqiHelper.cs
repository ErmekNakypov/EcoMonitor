namespace EcoMonitor.Web.Helpers;

public enum AqiLevel
{
    Good,
    Moderate,
    UnhealthyForSensitive,
    Unhealthy,
    VeryUnhealthy,
    Hazardous,
    Unknown
}

public static class AqiHelper
{
    // Host-supplied resolver wired in Program.cs to an IStringLocalizer
    // over SharedResource. Returns null when no resource hit, in which
    // case GetLabel falls back to the English switch below.
    public static Func<AqiLevel, string?>? LabelResolver { get; set; }

    public static AqiLevel ClassifyAqiUs(double? aqi)
    {
        if (aqi is null) return AqiLevel.Unknown;
        var a = aqi.Value;
        if (a <= 50) return AqiLevel.Good;
        if (a <= 100) return AqiLevel.Moderate;
        if (a <= 150) return AqiLevel.UnhealthyForSensitive;
        if (a <= 200) return AqiLevel.Unhealthy;
        if (a <= 300) return AqiLevel.VeryUnhealthy;
        return AqiLevel.Hazardous;
    }

    public static AqiLevel ClassifyPm25(double? pm25)
    {
        if (pm25 is null) return AqiLevel.Unknown;
        var v = pm25.Value;
        if (v < 12) return AqiLevel.Good;
        if (v < 35) return AqiLevel.Moderate;
        if (v < 55) return AqiLevel.UnhealthyForSensitive;
        if (v < 150) return AqiLevel.Unhealthy;
        if (v < 250) return AqiLevel.VeryUnhealthy;
        return AqiLevel.Hazardous;
    }

    public static string GetColorHex(AqiLevel level) => level switch
    {
        AqiLevel.Good => "#10B981",
        AqiLevel.Moderate => "#FCD34D",
        AqiLevel.UnhealthyForSensitive => "#FB923C",
        AqiLevel.Unhealthy => "#EF4444",
        AqiLevel.VeryUnhealthy => "#A855F7",
        AqiLevel.Hazardous => "#7F1D1D",
        _ => "#9CA3AF"
    };

    public static string GetLabel(AqiLevel level)
    {
        var localized = LabelResolver?.Invoke(level);
        if (!string.IsNullOrEmpty(localized)) return localized;

        return level switch
        {
            AqiLevel.Good => "Good",
            AqiLevel.Moderate => "Moderate",
            AqiLevel.UnhealthyForSensitive => "Unhealthy for sensitive groups",
            AqiLevel.Unhealthy => "Unhealthy",
            AqiLevel.VeryUnhealthy => "Very unhealthy",
            AqiLevel.Hazardous => "Hazardous",
            _ => "No data"
        };
    }
}
