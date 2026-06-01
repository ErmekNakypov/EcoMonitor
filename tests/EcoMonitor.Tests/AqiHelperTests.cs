using EcoMonitor.Web.Helpers;

namespace EcoMonitor.Tests;

public class AqiHelperTests
{
    [Fact]
    public void ClassifyAqiUs_Null_ReturnsUnknown()
    {
        Assert.Equal(AqiLevel.Unknown, AqiHelper.ClassifyAqiUs(null));
    }

    [Theory]
    [InlineData(0,   AqiLevel.Good)]
    [InlineData(50,  AqiLevel.Good)]                     // top of Good band
    [InlineData(51,  AqiLevel.Moderate)]                 // bottom of Moderate band
    [InlineData(100, AqiLevel.Moderate)]
    [InlineData(101, AqiLevel.UnhealthyForSensitive)]
    [InlineData(150, AqiLevel.UnhealthyForSensitive)]
    [InlineData(151, AqiLevel.Unhealthy)]
    [InlineData(200, AqiLevel.Unhealthy)]
    [InlineData(201, AqiLevel.VeryUnhealthy)]
    [InlineData(300, AqiLevel.VeryUnhealthy)]
    [InlineData(301, AqiLevel.Hazardous)]
    [InlineData(500, AqiLevel.Hazardous)]
    public void ClassifyAqiUs_BandCutoffs_AreCorrect(double aqi, AqiLevel expected)
    {
        Assert.Equal(expected, AqiHelper.ClassifyAqiUs(aqi));
    }

    [Fact]
    public void ClassifyPm25_Null_ReturnsUnknown()
    {
        Assert.Equal(AqiLevel.Unknown, AqiHelper.ClassifyPm25(null));
    }

    [Theory]
    [InlineData(0.0,   AqiLevel.Good)]
    [InlineData(11.9,  AqiLevel.Good)]                   // just under 12
    [InlineData(12.0,  AqiLevel.Moderate)]               // < 12 is Good, so 12 itself is Moderate
    [InlineData(34.9,  AqiLevel.Moderate)]
    [InlineData(35.0,  AqiLevel.UnhealthyForSensitive)]
    [InlineData(54.9,  AqiLevel.UnhealthyForSensitive)]
    [InlineData(55.0,  AqiLevel.Unhealthy)]
    [InlineData(149.9, AqiLevel.Unhealthy)]
    [InlineData(150.0, AqiLevel.VeryUnhealthy)]
    [InlineData(249.9, AqiLevel.VeryUnhealthy)]
    [InlineData(250.0, AqiLevel.Hazardous)]
    [InlineData(500.0, AqiLevel.Hazardous)]
    public void ClassifyPm25_BandCutoffs_AreCorrect(double pm25, AqiLevel expected)
    {
        Assert.Equal(expected, AqiHelper.ClassifyPm25(pm25));
    }

    [Theory]
    [InlineData(AqiLevel.Good,                  "#10B981")]
    [InlineData(AqiLevel.Moderate,              "#FCD34D")]
    [InlineData(AqiLevel.UnhealthyForSensitive, "#FB923C")]
    [InlineData(AqiLevel.Unhealthy,             "#EF4444")]
    [InlineData(AqiLevel.VeryUnhealthy,         "#A855F7")]
    [InlineData(AqiLevel.Hazardous,             "#7F1D1D")]
    [InlineData(AqiLevel.Unknown,               "#9CA3AF")]
    public void GetColorHex_ReturnsCorrectColor(AqiLevel level, string expected)
    {
        Assert.Equal(expected, AqiHelper.GetColorHex(level));
    }

    [Theory]
    [InlineData(AqiLevel.Good,                  "Good")]
    [InlineData(AqiLevel.Moderate,              "Moderate")]
    [InlineData(AqiLevel.UnhealthyForSensitive, "Unhealthy for sensitive groups")]
    [InlineData(AqiLevel.Unhealthy,             "Unhealthy")]
    [InlineData(AqiLevel.VeryUnhealthy,         "Very unhealthy")]
    [InlineData(AqiLevel.Hazardous,             "Hazardous")]
    [InlineData(AqiLevel.Unknown,               "No data")]
    public void GetLabel_ReturnsCorrectLabel(AqiLevel level, string expected)
    {
        Assert.Equal(expected, AqiHelper.GetLabel(level));
    }
}
