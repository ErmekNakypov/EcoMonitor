using System.Globalization;
using EcoMonitor.Web.Helpers;

namespace EcoMonitor.Tests;

public class AirAdvisoryTests
{
    // Levels for which the helper has a content branch (excludes Unknown,
    // which intentionally returns a "no data" placeholder).
    public static IEnumerable<object[]> ContentLevels => new[]
    {
        new object[] { AqiLevel.Good },
        new object[] { AqiLevel.Moderate },
        new object[] { AqiLevel.UnhealthyForSensitive },
        new object[] { AqiLevel.Unhealthy },
        new object[] { AqiLevel.VeryUnhealthy },
        new object[] { AqiLevel.Hazardous }
    };

    [Theory]
    [MemberData(nameof(ContentLevels))]
    public void Russian_IsNonEmpty_ForEveryContentLevel(AqiLevel level)
    {
        var text = AirAdvisory.InRussian(level);
        Assert.False(string.IsNullOrWhiteSpace(text), $"Empty Russian advisory for {level}");
    }

    [Theory]
    [MemberData(nameof(ContentLevels))]
    public void English_IsNonEmpty_ForEveryContentLevel(AqiLevel level)
    {
        var text = AirAdvisory.InEnglish(level);
        Assert.False(string.IsNullOrWhiteSpace(text), $"Empty English advisory for {level}");
    }

    [Theory]
    [MemberData(nameof(ContentLevels))]
    public void Kyrgyz_IsNonEmpty_ForEveryContentLevel(AqiLevel level)
    {
        var text = AirAdvisory.InKyrgyz(level);
        Assert.False(string.IsNullOrWhiteSpace(text), $"Empty Kyrgyz advisory for {level}");
    }

    [Fact]
    public void HazardousAdvisory_RecommendsStayingIndoors_AllLanguages()
    {
        // The hazardous-band copy must explicitly steer the user indoors.
        // Russian: "помещении" (inside premises). English: "indoor".
        // Kyrgyz:  "үйдө" (at home).
        Assert.Contains("помещении", AirAdvisory.InRussian(AqiLevel.Hazardous));
        Assert.Contains("indoors",   AirAdvisory.InEnglish(AqiLevel.Hazardous));
        Assert.Contains("үйдө",      AirAdvisory.InKyrgyz(AqiLevel.Hazardous));
    }

    [Fact]
    public void For_RussianCulture_ReturnsRussianText()
    {
        var ru = new CultureInfo("ru-RU");
        Assert.Equal(AirAdvisory.InRussian(AqiLevel.Good), AirAdvisory.For(AqiLevel.Good, ru));
    }

    [Fact]
    public void For_EnglishCulture_ReturnsEnglishText()
    {
        var en = new CultureInfo("en-US");
        Assert.Equal(AirAdvisory.InEnglish(AqiLevel.Moderate), AirAdvisory.For(AqiLevel.Moderate, en));
    }

    [Fact]
    public void For_KyrgyzCulture_ReturnsKyrgyzText()
    {
        var ky = new CultureInfo("ky-KG");
        Assert.Equal(AirAdvisory.InKyrgyz(AqiLevel.Unhealthy), AirAdvisory.For(AqiLevel.Unhealthy, ky));
    }

    [Fact]
    public void For_UnknownLanguage_FallsBackToRussian()
    {
        // The dispatcher's default branch is Russian; verify with a culture
        // outside the explicit en/ky branches.
        var de = new CultureInfo("de-DE");
        Assert.Equal(AirAdvisory.InRussian(AqiLevel.Good), AirAdvisory.For(AqiLevel.Good, de));
    }

    [Fact]
    public void Unknown_ReturnsPlaceholderInAllLanguages()
    {
        // The placeholders are also non-empty (they communicate "no data").
        Assert.False(string.IsNullOrWhiteSpace(AirAdvisory.InRussian(AqiLevel.Unknown)));
        Assert.False(string.IsNullOrWhiteSpace(AirAdvisory.InEnglish(AqiLevel.Unknown)));
        Assert.False(string.IsNullOrWhiteSpace(AirAdvisory.InKyrgyz(AqiLevel.Unknown)));
    }
}
