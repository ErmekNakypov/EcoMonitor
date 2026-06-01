using System.Text;
using EcoMonitor.Infrastructure.Districts;

namespace EcoMonitor.Tests;

// Targets the internal LoadFromStreamAsync overload added as a test seam in
// GeoJsonDistrictsLoader. The public LoadAsync still reads from disk and
// delegates to this same method, so behavioural parity is guaranteed.
public class GeoJsonDistrictsLoaderTests
{
    private static Stream JsonStream(string json) =>
        new MemoryStream(Encoding.UTF8.GetBytes(json));

    // --- Builder helpers for inline GeoJSON shapes --------------------------

    private static string ValidFeatureCollection() => """
    {
      "type": "FeatureCollection",
      "features": [
        { "type": "Feature", "properties": { "code": "LENIN",    "name_ru": "Ленинский" },
          "geometry": { "type": "Polygon", "coordinates":
            [[ [74.50, 42.80], [74.55, 42.80], [74.55, 42.85], [74.50, 42.85], [74.50, 42.80] ]] } },
        { "type": "Feature", "properties": { "code": "PERVOMAY", "name_ru": "Первомайский" },
          "geometry": { "type": "Polygon", "coordinates":
            [[ [74.60, 42.90], [74.65, 42.90], [74.65, 42.95], [74.60, 42.95], [74.60, 42.90] ]] } },
        { "type": "Feature", "properties": { "code": "SVERDLOV", "name_ru": "Свердловский" },
          "geometry": { "type": "Polygon", "coordinates":
            [[ [74.70, 42.80], [74.75, 42.80], [74.75, 42.85], [74.70, 42.85], [74.70, 42.80] ]] } },
        { "type": "Feature", "properties": { "code": "OKTYABR",  "name_ru": "Октябрьский" },
          "geometry": { "type": "Polygon", "coordinates":
            [[ [74.60, 42.80], [74.65, 42.80], [74.65, 42.85], [74.60, 42.85], [74.60, 42.80] ]] } }
      ]
    }
    """;

    // --- Happy path ---------------------------------------------------------

    [Fact]
    public async Task ValidShape_Returns4DistrictsWithSwappedLatLng_AndClosingDuplicateDropped()
    {
        await using var s = JsonStream(ValidFeatureCollection());
        var result = await GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>");

        Assert.Equal(4, result.Count);
        Assert.Equal(
            new HashSet<string> { "LENIN", "PERVOMAY", "SVERDLOV", "OKTYABR" },
            result.Select(f => f.Code).ToHashSet());

        // Every test ring has 5 vertices (closing duplicate); loader drops it.
        Assert.All(result, f => Assert.Equal(4, f.Vertices.Count));

        var lenin = result.Single(f => f.Code == "LENIN");
        // First GeoJSON point was [74.50, 42.80] (i.e. [lng, lat]).
        // Loader must swap to (Lat: 42.80, Lng: 74.50).
        Assert.Equal(42.80, lenin.Vertices[0].Lat, precision: 6);
        Assert.Equal(74.50, lenin.Vertices[0].Lng, precision: 6);
        Assert.Equal("Ленинский", lenin.NameRu);
    }

    // --- Validation failure cases ------------------------------------------

    [Fact]
    public async Task ThreeFeatures_ThrowsWithCountInMessage()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [
            { "type": "Feature", "properties": { "code": "LENIN" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.50, 42.80], [74.55, 42.80], [74.55, 42.85], [74.50, 42.80] ]] } },
            { "type": "Feature", "properties": { "code": "PERVOMAY" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.60, 42.90], [74.65, 42.90], [74.65, 42.95], [74.60, 42.90] ]] } },
            { "type": "Feature", "properties": { "code": "SVERDLOV" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.70, 42.80], [74.75, 42.80], [74.75, 42.85], [74.70, 42.80] ]] } }
          ]
        }
        """;
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        Assert.Contains("4 features", ex.Message);
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public async Task UnknownCode_ThrowsWithCodeInMessage()
    {
        var json = ValidFeatureCollection().Replace("\"OKTYABR\"", "\"WRONG\"");
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        Assert.Contains("WRONG", ex.Message);
    }

    [Fact]
    public async Task DuplicateCode_Throws()
    {
        // Replace OKTYABR with a duplicate LENIN — should fail the duplicate-codes rule.
        var json = ValidFeatureCollection().Replace("\"OKTYABR\"", "\"LENIN\"");
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        // Either duplicate-code error OR membership error is acceptable.
        Assert.True(
            ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("LENIN"),
            $"Expected duplicate-code error, got: {ex.Message}");
    }

    [Fact]
    public async Task MultiPolygonGeometry_Throws()
    {
        var json = ValidFeatureCollection().Replace("\"Polygon\"", "\"MultiPolygon\"");
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        Assert.Contains("Polygon", ex.Message);
    }

    [Fact]
    public async Task RingWithFewerThan4Points_Throws()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [
            { "type": "Feature", "properties": { "code": "LENIN" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.50, 42.80], [74.55, 42.80], [74.50, 42.80] ]] } },
            { "type": "Feature", "properties": { "code": "PERVOMAY" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.60, 42.90], [74.65, 42.90], [74.65, 42.95], [74.60, 42.95], [74.60, 42.90] ]] } },
            { "type": "Feature", "properties": { "code": "SVERDLOV" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.70, 42.80], [74.75, 42.80], [74.75, 42.85], [74.70, 42.85], [74.70, 42.80] ]] } },
            { "type": "Feature", "properties": { "code": "OKTYABR" },
              "geometry": { "type": "Polygon", "coordinates":
                [[ [74.60, 42.80], [74.65, 42.80], [74.65, 42.85], [74.60, 42.85], [74.60, 42.80] ]] } }
          ]
        }
        """;
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        Assert.Contains("at least 4", ex.Message);
    }

    [Fact]
    public async Task WrongTopLevelType_Throws()
    {
        // FeatureCollection swapped to plain Feature.
        var json = ValidFeatureCollection().Replace("\"FeatureCollection\"", "\"Feature\"");
        await using var s = JsonStream(json);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
        Assert.Contains("FeatureCollection", ex.Message);
    }

    [Fact]
    public async Task MalformedJson_Throws()
    {
        await using var s = JsonStream("{ this is not valid json");
        await Assert.ThrowsAsync<InvalidDataException>(
            () => GeoJsonDistrictsLoader.LoadFromStreamAsync(s, "<inline>"));
    }
}
