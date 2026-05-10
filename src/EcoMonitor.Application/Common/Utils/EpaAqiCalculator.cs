namespace EcoMonitor.Application.Common.Utils;

public static class EpaAqiCalculator
{
    /// <summary>
    /// Converts 24-hour PM2.5 concentration in µg/m³ to US EPA AQI value (0–500).
    /// EPA breakpoints: https://www.airnow.gov/aqi/aqi-calculator-concentration/
    /// </summary>
    public static double? Pm25ToAqi(double? pm25)
    {
        if (pm25 is null || pm25 < 0) return null;
        var c = pm25.Value;

        var bp = new (double cl, double ch, double il, double ih)[]
        {
            (0.0,   12.0,   0,   50),
            (12.1,  35.4,   51,  100),
            (35.5,  55.4,   101, 150),
            (55.5,  150.4,  151, 200),
            (150.5, 250.4,  201, 300),
            (250.5, 350.4,  301, 400),
            (350.5, 500.4,  401, 500),
        };

        foreach (var (cl, ch, il, ih) in bp)
        {
            if (c >= cl && c <= ch)
            {
                return Math.Round(((ih - il) / (ch - cl)) * (c - cl) + il);
            }
        }

        return c > 500.4 ? 500 : null;
    }
}
