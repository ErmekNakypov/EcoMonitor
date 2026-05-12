namespace EcoMonitor.Application.Common.Services;

// Ray-casting point-in-polygon. Treats lat/lng as planar coordinates — fine
// for a single city's scale (~30 km across) where projection distortion is
// negligible. Polygon vertices may be ordered either clockwise or
// counter-clockwise; the algorithm doesn't care about winding order.
public static class PointInPolygonChecker
{
    public static bool IsPointInside(double lat, double lng, IReadOnlyList<(double Lat, double Lng)> polygon)
    {
        if (polygon.Count < 3) return false;

        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            // Ray from (lat, lng) cast horizontally (east) crossing edge (pi, pj).
            if (((pi.Lat > lat) != (pj.Lat > lat)) &&
                (lng < (pj.Lng - pi.Lng) * (lat - pi.Lat) / (pj.Lat - pi.Lat) + pi.Lng))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
