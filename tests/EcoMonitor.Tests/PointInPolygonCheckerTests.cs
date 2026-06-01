using EcoMonitor.Application.Common.Services;

namespace EcoMonitor.Tests;

public class PointInPolygonCheckerTests
{
    // 1×1 square at origin, CCW ordering.
    private static readonly IReadOnlyList<(double Lat, double Lng)> UnitSquare =
        new[] { (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0) };

    // L-shape: the missing concave notch is the (lat ∈ (0.5,1], lng ∈ (0.5,1]) quadrant.
    //
    //   1.0  +-------+
    //        |       |
    //        |  arm  |
    //   0.5  +---+   +
    //        |arm|   . <- (0.75, 0.75) is in the "notch" → outside the L
    //   0.0  +---+---+
    //       0.0 0.5 1.0
    private static readonly IReadOnlyList<(double Lat, double Lng)> LShape =
        new[]
        {
            (0.0, 0.0),
            (1.0, 0.0),
            (1.0, 0.5),
            (0.5, 0.5),
            (0.5, 1.0),
            (0.0, 1.0)
        };

    [Fact]
    public void PointInsideSquare_ReturnsTrue()
    {
        Assert.True(PointInPolygonChecker.IsPointInside(0.5, 0.5, UnitSquare));
    }

    [Fact]
    public void PointOutsideSquare_ReturnsFalse()
    {
        Assert.False(PointInPolygonChecker.IsPointInside(5.0, 5.0, UnitSquare));
        Assert.False(PointInPolygonChecker.IsPointInside(-0.1, 0.5, UnitSquare));
        Assert.False(PointInPolygonChecker.IsPointInside(0.5, 1.1, UnitSquare));
    }

    [Fact]
    public void PointJustOutsideEdge_ReturnsFalse()
    {
        // Slightly to the right of the square's east edge (lng = 1.0).
        Assert.False(PointInPolygonChecker.IsPointInside(0.5, 1.0001, UnitSquare));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PolygonWithFewerThan3Vertices_ReturnsFalse(int vertexCount)
    {
        var polygon = new List<(double, double)>();
        for (var i = 0; i < vertexCount; i++) polygon.Add((i, i));
        Assert.False(PointInPolygonChecker.IsPointInside(0.5, 0.5, polygon));
    }

    [Fact]
    public void PointInLShapeArm_ReturnsTrue()
    {
        // (0.25, 0.25) is in the bottom-left arm of the L — clearly inside.
        Assert.True(PointInPolygonChecker.IsPointInside(0.25, 0.25, LShape));
        // (0.25, 0.75) is in the upper arm — also inside.
        Assert.True(PointInPolygonChecker.IsPointInside(0.25, 0.75, LShape));
    }

    [Fact]
    public void PointInLShapeNotch_ReturnsFalse()
    {
        // (0.75, 0.75) is in the concave notch — geometrically OUTSIDE the L.
        // This is the crucial assertion: a naive bounding-box check would
        // wrongly say "inside"; the ray-casting algorithm correctly rejects.
        Assert.False(PointInPolygonChecker.IsPointInside(0.75, 0.75, LShape));
    }
}
