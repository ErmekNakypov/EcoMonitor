namespace EcoMonitor.Infrastructure.Containers;

internal static class BishkekDistrictResolver
{
    // Bishkek has four administrative districts. Approximate quadrant split by
    // (lat, lng) — good enough for display labeling, not for analytics.
    //   Horizontal divider (north/south): south side of the main railway line
    //   Vertical divider   (west/east):   along the Alamedin river
    private const double LatDivider = 42.870;
    private const double LngDivider = 74.595;

    public static string Resolve(double lat, double lng)
    {
        var isNorth = lat > LatDivider;
        var isEast = lng > LngDivider;

        return (isNorth, isEast) switch
        {
            (true, false) => "Sverdlovsky district",
            (true, true) => "Pervomaisky district",
            (false, false) => "Leninsky district",
            (false, true) => "Oktyabrsky district"
        };
    }
}
