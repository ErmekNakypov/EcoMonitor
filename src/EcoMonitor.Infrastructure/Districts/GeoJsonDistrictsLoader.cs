using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoMonitor.Infrastructure.Districts;

// Loads the four Bishkek raion polygons from the bundled GeoJSON snapshot at
// Districts/Snapshots/bishkek-districts.geojson, validates the shape strictly,
// and returns (Lat, Lng) vertex arrays in the order the entity expects.
//
// The on-disk file is a GeoJSON FeatureCollection of exactly 4 single-ring
// Polygon features with properties.code in {LENIN, PERVOMAY, SVERDLOV, OKTYABR}.
// GeoJSON coordinate order is [lng, lat] per RFC 7946; this loader does the
// swap. The hand-tuned in-code seeder stores vertices WITHOUT the closing
// duplicate point (4-vertex rectangles, not 5), so we match that convention
// here and drop the trailing closing point if the ring is closed.
internal static class GeoJsonDistrictsLoader
{
    private const string SnapshotRelativePath = "Districts/Snapshots/bishkek-districts.geojson";

    public static readonly IReadOnlySet<string> ExpectedCodes =
        new HashSet<string> { "LENIN", "PERVOMAY", "SVERDLOV", "OKTYABR" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed record DistrictFeature(string Code, string? NameRu, IReadOnlyList<(double Lat, double Lng)> Vertices);

    public static async Task<IReadOnlyList<DistrictFeature>> LoadAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, SnapshotRelativePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"District boundaries snapshot not found at '{path}'. " +
                "Ensure the file is included as Content with CopyToOutputDirectory in EcoMonitor.Infrastructure.csproj.",
                path);
        }

        await using var stream = File.OpenRead(path);
        return await LoadFromStreamAsync(stream, path, ct);
    }

    // Test seam: the parse + validation logic without the disk-read step.
    // Production callers go through LoadAsync(); tests feed a MemoryStream
    // built from an inline JSON string to exercise the validation rules
    // without touching the filesystem. Behavioural parity is guaranteed
    // because LoadAsync delegates here.
    internal static async Task<IReadOnlyList<DistrictFeature>> LoadFromStreamAsync(
        Stream stream, string source, CancellationToken ct = default)
    {
        FeatureCollectionDto? parsed;
        try
        {
            parsed = await JsonSerializer.DeserializeAsync<FeatureCollectionDto>(stream, JsonOptions, ct);
        }
        catch (JsonException jx)
        {
            throw new InvalidDataException(
                $"District boundaries snapshot at '{source}' is not valid JSON: {jx.Message}");
        }

        if (parsed is null)
        {
            throw new InvalidDataException($"District boundaries snapshot at '{source}' is empty or unparseable JSON.");
        }
        if (!string.Equals(parsed.Type, "FeatureCollection", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"District boundaries snapshot must be a FeatureCollection; got '{parsed.Type ?? "<null>"}'.");
        }
        if (parsed.Features is null || parsed.Features.Count != 4)
        {
            throw new InvalidDataException(
                $"District boundaries snapshot must contain exactly 4 features; got {parsed.Features?.Count ?? 0}.");
        }

        var result = new List<DistrictFeature>(4);
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < parsed.Features.Count; i++)
        {
            var feat = parsed.Features[i];
            var code = feat.Properties?.Code?.Trim() ?? string.Empty;

            if (!ExpectedCodes.Contains(code))
            {
                throw new InvalidDataException(
                    $"Feature {i}: properties.code '{code}' is not one of LENIN/PERVOMAY/SVERDLOV/OKTYABR.");
            }
            if (!seenCodes.Add(code))
            {
                throw new InvalidDataException($"Feature {i}: duplicate code '{code}'.");
            }
            if (feat.Geometry is null
                || !string.Equals(feat.Geometry.Type, "Polygon", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Feature {i} ({code}): geometry.type must be 'Polygon'; got '{feat.Geometry?.Type ?? "<null>"}'. " +
                    "MultiPolygon is not supported by this loader.");
            }

            var rings = feat.Geometry.Coordinates;
            if (rings is null || rings.Count == 0 || rings[0] is null)
            {
                throw new InvalidDataException($"Feature {i} ({code}): geometry has no rings.");
            }

            var outerRing = rings[0]!;
            if (outerRing.Count < 4)
            {
                throw new InvalidDataException(
                    $"Feature {i} ({code}): outer ring has only {outerRing.Count} point(s); need at least 4.");
            }

            // GeoJSON [lng, lat] → entity (Lat, Lng).
            var vertices = new List<(double Lat, double Lng)>(outerRing.Count);
            for (var v = 0; v < outerRing.Count; v++)
            {
                var pt = outerRing[v];
                if (pt is null || pt.Count < 2)
                {
                    throw new InvalidDataException(
                        $"Feature {i} ({code}): vertex {v} is not a [lng, lat] pair.");
                }
                vertices.Add((Lat: pt[1], Lng: pt[0]));
            }

            // Drop the closing duplicate if the ring is GeoJSON-closed
            // (first == last). The entity convention is an "open" ring;
            // the resolver's ray-casting and Leaflet's L.polygon both
            // close the ring implicitly.
            if (vertices.Count > 1
                && vertices[0].Lat == vertices[^1].Lat
                && vertices[0].Lng == vertices[^1].Lng)
            {
                vertices.RemoveAt(vertices.Count - 1);
            }

            if (vertices.Count < 3)
            {
                throw new InvalidDataException(
                    $"Feature {i} ({code}): after dropping closing point, only {vertices.Count} vertex/vertices remain; need >= 3 for a polygon.");
            }

            result.Add(new DistrictFeature(code, feat.Properties?.NameRu, vertices));
        }

        // Final completeness check: all four codes present (covered above by
        // the count + per-feature membership check, but be explicit).
        var missing = ExpectedCodes.Except(seenCodes).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException(
                $"District boundaries snapshot is missing required codes: {string.Join(", ", missing)}.");
        }

        return result;
    }

    private sealed class FeatureCollectionDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("features")] public List<FeatureDto>? Features { get; set; }
    }

    private sealed class FeatureDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("properties")] public PropertiesDto? Properties { get; set; }
        [JsonPropertyName("geometry")] public GeometryDto? Geometry { get; set; }
    }

    private sealed class PropertiesDto
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name_ru")] public string? NameRu { get; set; }
    }

    private sealed class GeometryDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        // Polygon → list of rings → list of [lng,lat] pairs.
        [JsonPropertyName("coordinates")] public List<List<List<double>>?>? Coordinates { get; set; }
    }
}
