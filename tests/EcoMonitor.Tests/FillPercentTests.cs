using EcoMonitor.Application.Features.Sensors.IngestContainerFillReading;

namespace EcoMonitor.Tests;

// Targets the internal static IngestContainerFillReadingHandler.ComputeFillPercent.
// Visibility: see InternalsVisibleTo("EcoMonitor.Tests") in
// EcoMonitor.Application.csproj. The method was previously `private static`;
// changed to `internal static` for unit-testability with no behavioural impact.
public class FillPercentTests
{
    [Theory]
    [InlineData(50, 25, 50)]   // half-empty bin
    [InlineData(50,  0, 100)]  // distance 0 → totally full
    [InlineData(50, 50,  0)]   // distance == height → empty
    public void NormalRange_ComputesExpectedPercent(double height, double distance, double expected)
    {
        var result = IngestContainerFillReadingHandler.ComputeFillPercent(height, distance);
        Assert.Equal(expected, result, precision: 6);
    }

    [Fact]
    public void DistanceGreaterThanHeight_ClampsToZero()
    {
        var result = IngestContainerFillReadingHandler.ComputeFillPercent(50, 60);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void NegativeDistance_ClampsTo100()
    {
        // Defensive: an overshooting sensor (distance < 0) must not produce
        // a value > 100 % downstream.
        var result = IngestContainerFillReadingHandler.ComputeFillPercent(50, -5);
        Assert.Equal(100.0, result);
    }

    [Theory]
    [InlineData(100, 10, 90)]  // 10cm down in a 100cm bin → 90 % full
    [InlineData(120, 60, 50)]
    public void DifferentBinHeights_RespectIndividualHeight(double height, double distance, double expected)
    {
        var result = IngestContainerFillReadingHandler.ComputeFillPercent(height, distance);
        Assert.Equal(expected, result, precision: 6);
    }
}
