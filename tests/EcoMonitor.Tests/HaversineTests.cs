using EcoMonitor.Application.Common.Services.Routing;

namespace EcoMonitor.Tests;

public class HaversineTests
{
    [Fact]
    public void SamePointTwice_IsZero()
    {
        var d = RouteCalculator.Haversine(42.8746, 74.5698, 42.8746, 74.5698);
        Assert.Equal(0.0, d, precision: 6);
    }

    [Fact]
    public void IsSymmetric()
    {
        // Pick two arbitrary city-scale points.
        var ab = RouteCalculator.Haversine(42.8746, 74.5698, 42.9100, 74.5500);
        var ba = RouteCalculator.Haversine(42.9100, 74.5500, 42.8746, 74.5698);
        Assert.Equal(ab, ba, precision: 9);
    }

    [Fact]
    public void MoscowToStPetersburg_IsApproximately635km()
    {
        // Reference straight-line (great-circle) distance: ~634.6 km.
        // We accept ±0.5 % — the textbook tolerance for Haversine vs the
        // ellipsoidal-Earth model at continental scale.
        var moscow = (Lat: 55.7558, Lng: 37.6173);
        var spb    = (Lat: 59.9343, Lng: 30.3351);

        var km = RouteCalculator.Haversine(moscow.Lat, moscow.Lng, spb.Lat, spb.Lng);

        Assert.InRange(km, 634.6 * 0.995, 634.6 * 1.005);
    }

    [Fact]
    public void BishkekScaleDistance_IsPlausible()
    {
        // Two points ~5 km apart inside Bishkek — sanity check for the
        // city scale we actually route across.
        var d = RouteCalculator.Haversine(42.8746, 74.5698, 42.9200, 74.5698);
        // 0.0454° latitude ≈ 5.05 km — assert generous tolerance.
        Assert.InRange(d, 4.8, 5.3);
    }
}
