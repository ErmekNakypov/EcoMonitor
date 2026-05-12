namespace EcoMonitor.Application.Common.Services.Routing;

// Geographic distance + nearest-neighbor TSP heuristic for visit-route
// planning. Polynomial-time approximation, ~25% above optimal on average
// for the city-scale node counts (≤ 15) we cap routes at. Treats lat/lng
// as planar over a single city is fine — for Bishkek's ~30 km diameter
// the great-circle approximation Haversine already accounts for.
public static class RouteCalculator
{
    private const double EarthRadiusKm = 6371.0;
    private const double AverageUrbanSpeedKmh = 25.0;

    public static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    // Nearest-neighbor: start from index 0, repeatedly visit the closest
    // unvisited point. O(n^2) — fine for the route-size cap.
    public static List<int> NearestNeighborOrder(IReadOnlyList<(double Lat, double Lng)> points)
    {
        if (points.Count == 0) return new List<int>();
        if (points.Count == 1) return new List<int> { 0 };

        var visited = new bool[points.Count];
        var order = new List<int>(points.Count) { 0 };
        visited[0] = true;

        var current = 0;
        for (var step = 1; step < points.Count; step++)
        {
            var nearest = -1;
            var bestDistance = double.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                if (visited[i]) continue;
                var d = Haversine(points[current].Lat, points[current].Lng,
                                  points[i].Lat, points[i].Lng);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    nearest = i;
                }
            }
            visited[nearest] = true;
            order.Add(nearest);
            current = nearest;
        }
        return order;
    }

    public static double TotalDistance(IReadOnlyList<(double Lat, double Lng)> orderedPoints)
    {
        var total = 0.0;
        for (var i = 0; i < orderedPoints.Count - 1; i++)
        {
            total += Haversine(
                orderedPoints[i].Lat, orderedPoints[i].Lng,
                orderedPoints[i + 1].Lat, orderedPoints[i + 1].Lng);
        }
        return total;
    }

    public static int EstimatedMinutes(double distanceKm)
        => (int)Math.Round(distanceKm / AverageUrbanSpeedKmh * 60.0);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
