using EcoMonitor.Application.Common.Services.Routing;

namespace EcoMonitor.Tests;

public class NearestNeighborTests
{
    [Fact]
    public void EmptyList_ReturnsEmptyOrder()
    {
        var order = RouteCalculator.NearestNeighborOrder(Array.Empty<(double, double)>());
        Assert.Empty(order);
    }

    [Fact]
    public void SinglePoint_ReturnsSingletonZero()
    {
        var order = RouteCalculator.NearestNeighborOrder(new[] { (42.0, 74.0) });
        Assert.Equal(new[] { 0 }, order);
    }

    [Fact]
    public void UnambiguousColinearOrder_ReturnsExpectedSequence()
    {
        // Three points roughly W → middle → E. Starting at index 0 (W), the
        // nearest is the middle point (index 1), then the east point (index 2).
        var points = new[]
        {
            (42.870, 74.500),  // 0 — west
            (42.870, 74.550),  // 1 — middle
            (42.870, 74.620)   // 2 — east
        };

        var order = RouteCalculator.NearestNeighborOrder(points);

        Assert.Equal(new[] { 0, 1, 2 }, order);
    }

    [Fact]
    public void ClusteredPlusOutlier_StrandsOutlierLast()
    {
        // Three central points within ~1 km of each other, plus one ~20 km away.
        // Documents the known greedy-NN weakness: starting at index 0, the
        // algorithm always picks the next-closest, so the far point gets
        // visited last (one long final leg). This is expected, not a bug.
        var points = new[]
        {
            (42.870, 74.595),  // 0 — central seed
            (42.875, 74.600),  // 1 — central (close to 0)
            (42.872, 74.605),  // 2 — central (close to 0 & 1)
            (42.870, 74.820)   // 3 — far east outlier
        };

        var order = RouteCalculator.NearestNeighborOrder(points);

        Assert.Equal(4, order.Count);
        Assert.Equal(0, order[0]);                            // seeds from index 0
        Assert.Equal(3, order[^1]);                           // outlier last
        Assert.Equal(new HashSet<int> { 0, 1, 2, 3 }, order.ToHashSet());
    }

    [Fact]
    public void TotalDistance_MatchesHandComputedSum()
    {
        // Three points along a longitude line; total distance for the
        // visit order 0 → 1 → 2 is Hav(0,1) + Hav(1,2).
        var p0 = (42.870, 74.500);
        var p1 = (42.870, 74.550);
        var p2 = (42.870, 74.620);

        var expected =
            RouteCalculator.Haversine(p0.Item1, p0.Item2, p1.Item1, p1.Item2) +
            RouteCalculator.Haversine(p1.Item1, p1.Item2, p2.Item1, p2.Item2);

        var actual = RouteCalculator.TotalDistance(new[] { p0, p1, p2 });

        Assert.Equal(expected, actual, precision: 9);
    }

    [Theory]
    [InlineData(0.0, 0)]    // 0 km / 25 km/h → 0 min
    [InlineData(25.0, 60)]  // exactly one hour
    [InlineData(12.5, 30)]  // half hour
    [InlineData(5.0, 12)]   // 5 / 25 * 60 = 12
    public void EstimatedMinutes_AppliesAverageSpeed(double km, int expectedMinutes)
    {
        Assert.Equal(expectedMinutes, RouteCalculator.EstimatedMinutes(km));
    }
}
