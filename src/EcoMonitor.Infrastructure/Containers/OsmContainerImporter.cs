using System.Text.Json;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Containers;

public sealed class OsmContainerImporter : IContainerImportService
{
    private const double BboxSouth = 42.83;
    private const double BboxWest = 74.50;
    private const double BboxNorth = 42.92;
    private const double BboxEast = 74.70;

    private const string LiveOverpassUrl = "https://overpass-api.de/api/interpreter";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ApplicationDbContext _db;
    private readonly IDistrictResolver _districtResolver;
    private readonly ILogger<OsmContainerImporter> _logger;

    public OsmContainerImporter(
        IHttpClientFactory httpFactory,
        ApplicationDbContext db,
        IDistrictResolver districtResolver,
        ILogger<OsmContainerImporter> logger)
    {
        _httpFactory = httpFactory;
        _db = db;
        _districtResolver = districtResolver;
        _logger = logger;
    }

    public async Task<ContainerImportResult> ImportFromOsmAsync(CancellationToken ct = default)
    {
        try
        {
            var query = $@"[out:json][timeout:60];
(
  node[""amenity""=""waste_basket""]({BboxSouth},{BboxWest},{BboxNorth},{BboxEast});
  node[""amenity""=""recycling""]({BboxSouth},{BboxWest},{BboxNorth},{BboxEast});
);
out body;";

            var (nodeElements, source) = await FetchElementsAsync(query, ct);

            int created = 0, updated = 0, skipped = 0;

            var osmIds = nodeElements.Select(e => e.Id).ToHashSet();
            var existing = await _db.WasteContainers
                .Where(c => c.OsmId != null && osmIds.Contains(c.OsmId.Value))
                .ToDictionaryAsync(c => c.OsmId!.Value, ct);

            var existingOsmCodes = await _db.WasteContainers
                .Where(c => c.Code.StartsWith("OSM-"))
                .Select(c => c.Code)
                .ToListAsync(ct);

            var nextNumber = 1;
            if (existingOsmCodes.Count > 0)
            {
                nextNumber = existingOsmCodes
                    .Select(c => int.TryParse(c.AsSpan(4), out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;
            }

            foreach (var el in nodeElements)
            {
                if (el.Type != "node")
                {
                    skipped++;
                    continue;
                }

                var tags = el.Tags ?? new Dictionary<string, string>();
                var type = MapOsmToType(tags);
                var address = BuildAddress(tags, el.Lat, el.Lon);

                // Resolve the district once per row — the resolver caches all
                // four polygons in memory, so 500 importer iterations cost one
                // DB hit total. New rows get DistrictId set; existing rows are
                // updated if their lat/lng moved (rare, but cheap).
                var district = await _districtResolver.ResolveAsync(el.Lat, el.Lon, ct);

                if (existing.TryGetValue(el.Id, out var container))
                {
                    container.Latitude = el.Lat;
                    container.Longitude = el.Lon;
                    container.Type = type;
                    container.Address = address;
                    container.DistrictId = district?.Id;
                    updated++;
                }
                else
                {
                    var newContainer = new WasteContainer
                    {
                        OsmId = el.Id,
                        IsImported = true,
                        Code = $"OSM-{nextNumber:D5}",
                        Address = address,
                        Latitude = el.Lat,
                        Longitude = el.Lon,
                        Type = type,
                        Capacity = type == ContainerType.General ? 240 : 660,
                        Status = ContainerStatus.Active,
                        InstalledAt = DateTime.UtcNow.AddYears(-1),
                        DistrictId = district?.Id
                    };
                    _db.WasteContainers.Add(newContainer);
                    nextNumber++;
                    created++;
                }
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "OSM container import ({Source}): fetched {Total}, created {Created}, updated {Updated}, skipped {Skipped}",
                source, nodeElements.Count, created, updated, skipped);

            return new ContainerImportResult(nodeElements.Count, created, updated, skipped, null, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSM container import failed");
            return new ContainerImportResult(0, 0, 0, 0, ex.Message);
        }
    }

    private async Task<(List<OverpassElement> Elements, string Source)> FetchElementsAsync(string query, CancellationToken ct)
    {
        try
        {
            var liveElements = await TryFetchLiveAsync(query, ct);
            if (liveElements is { Count: > 0 })
            {
                _logger.LogInformation("OSM import: using live Overpass data ({Count} elements)", liveElements.Count);
                return (liveElements, "live");
            }
            _logger.LogWarning("OSM import: live Overpass returned no usable data, falling back to snapshot");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSM import: live Overpass call failed, falling back to snapshot");
        }

        var snapshotElements = await LoadSnapshotAsync(ct);
        if (snapshotElements is null || snapshotElements.Count == 0)
        {
            throw new InvalidOperationException(
                "Both live Overpass and bundled snapshot failed. Snapshot file may be missing or invalid.");
        }

        _logger.LogInformation("OSM import: using bundled snapshot ({Count} elements)", snapshotElements.Count);
        return (snapshotElements, "snapshot");
    }

    private async Task<List<OverpassElement>?> TryFetchLiveAsync(string query, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("overpass");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("data", query)
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, LiveOverpassUrl) { Content = content };
        request.Headers.UserAgent.ParseAdd("EcoMonitor/1.0 (academic project, contact via github)");

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Live Overpass returned HTTP {Status}", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var parsed = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream, JsonOptions, ct);
        return parsed?.Elements?.Where(e => e.Type == "node").ToList();
    }

    private async Task<List<OverpassElement>?> LoadSnapshotAsync(CancellationToken ct)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Containers", "Snapshots", "bishkek-containers.json");
            if (!File.Exists(path))
            {
                _logger.LogError("OSM snapshot file not found at {Path}", path);
                return null;
            }
            await using var stream = File.OpenRead(path);
            var parsed = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream, JsonOptions, ct);
            return parsed?.Elements?.Where(e => e.Type == "node").ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OSM snapshot from disk");
            return null;
        }
    }

    private static ContainerType MapOsmToType(IDictionary<string, string> tags)
    {
        bool Has(string key) => tags.TryGetValue(key, out var v) && v == "yes";

        if (Has("recycling:plastic") || Has("recycling:plastic_packaging") || Has("recycling:plastic_bottles"))
            return ContainerType.Plastic;
        if (Has("recycling:glass") || Has("recycling:glass_bottles"))
            return ContainerType.Glass;
        if (Has("recycling:paper") || Has("recycling:cardboard") || Has("recycling:newspaper"))
            return ContainerType.Paper;
        if (Has("recycling:organic") || Has("recycling:green_waste"))
            return ContainerType.Organic;

        return ContainerType.General;
    }

    private static string BuildAddress(IDictionary<string, string>? tags, double lat, double lng)
    {
        var parts = new List<string>();

        if (tags is not null)
        {
            if (tags.TryGetValue("addr:street", out var street)) parts.Add(street);
            if (tags.TryGetValue("addr:housenumber", out var no)) parts.Add(no);
            if (tags.TryGetValue("name", out var name)) parts.Add($"({name})");
        }

        if (parts.Count > 0)
        {
            return string.Join(" ", parts);
        }

        var district = BishkekDistrictResolver.Resolve(lat, lng);
        return $"{district}, {lat:F4}, {lng:F4}";
    }

    private sealed class OverpassResponse
    {
        public List<OverpassElement>? Elements { get; set; }
    }

    private sealed class OverpassElement
    {
        public string Type { get; set; } = string.Empty;
        public long Id { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }
}
