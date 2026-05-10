using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class OpenAqAirQualityProvider : IAirQualityProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAqAirQualityProvider> _logger;

    public string Name => "OpenAq";

    public OpenAqAirQualityProvider(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<OpenAqAirQualityProvider> logger)
    {
        _httpFactory = httpFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderStationReading>> FetchCurrentReadingsAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration["OpenAq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAQ API key is not configured. Set OpenAq:ApiKey in user secrets.");
            return Array.Empty<ProviderStationReading>();
        }

        var client = _httpFactory.CreateClient("openaq");
        client.DefaultRequestHeaders.Remove("X-API-Key");
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        OpenAqLocationsResponse? locationsResp;
        try
        {
            locationsResp = await client.GetFromJsonAsync<OpenAqLocationsResponse>(
                "v3/locations?coordinates=42.8746,74.5698&radius=25000&limit=50", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenAQ locations call failed");
            return Array.Empty<ProviderStationReading>();
        }

        if (locationsResp?.Results is null || locationsResp.Results.Count == 0)
        {
            _logger.LogInformation("OpenAQ returned no locations near Bishkek.");
            return Array.Empty<ProviderStationReading>();
        }

        var nearest = locationsResp.Results
            .OrderBy(r => r.Distance ?? double.MaxValue)
            .Take(10)
            .ToList();

        var readings = new List<ProviderStationReading>();

        foreach (var loc in nearest)
        {
            try
            {
                var latestResp = await client.GetFromJsonAsync<OpenAqLatestResponse>(
                    $"v3/locations/{loc.Id}/latest", ct);

                if (latestResp?.Results is null || latestResp.Results.Count == 0)
                {
                    continue;
                }

                var sensorParameterMap = loc.Sensors?
                    .ToDictionary(s => s.Id, s => s.Parameter?.Name?.ToLowerInvariant() ?? string.Empty)
                    ?? new Dictionary<int, string>();

                double? pm25 = null, pm10 = null, temperature = null, humidity = null, pressure = null;
                DateTime? measuredAt = null;

                foreach (var result in latestResp.Results)
                {
                    if (!sensorParameterMap.TryGetValue(result.SensorsId, out var paramName))
                    {
                        continue;
                    }

                    var value = result.Value;
                    var dateUtc = result.Datetime?.Utc;
                    if (dateUtc.HasValue)
                    {
                        if (measuredAt is null || dateUtc.Value > measuredAt.Value)
                        {
                            measuredAt = dateUtc.Value;
                        }
                    }

                    switch (paramName)
                    {
                        case "pm25": pm25 = value; break;
                        case "pm10": pm10 = value; break;
                        case "temperature": temperature = value; break;
                        case "relativehumidity":
                        case "humidity": humidity = value; break;
                        case "pressure": pressure = value; break;
                    }
                }

                if (measuredAt is null)
                {
                    continue;
                }

                readings.Add(new ProviderStationReading(
                    ExternalId: loc.Id.ToString(),
                    StationName: loc.Name ?? $"Station {loc.Id}",
                    Locality: loc.Locality,
                    ProviderName: loc.Provider?.Name,
                    Latitude: loc.Coordinates?.Latitude ?? 0,
                    Longitude: loc.Coordinates?.Longitude ?? 0,
                    Source: AirQualitySource.External,
                    Pm25: pm25,
                    Pm10: pm10,
                    Temperature: temperature,
                    Humidity: humidity,
                    Pressure: pressure,
                    MeasuredAt: measuredAt.Value));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch latest for location {LocationId}", loc.Id);
            }

            try
            {
                await Task.Delay(150, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "OpenAQ fetched {Count} station readings out of {Total} nearby locations",
            readings.Count, nearest.Count);

        return readings;
    }

    private sealed class OpenAqLocationsResponse
    {
        [JsonPropertyName("results")] public List<OpenAqLocation>? Results { get; set; }
    }

    private sealed class OpenAqLocation
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("locality")] public string? Locality { get; set; }
        [JsonPropertyName("coordinates")] public OpenAqCoordinates? Coordinates { get; set; }
        [JsonPropertyName("provider")] public OpenAqProvider? Provider { get; set; }
        [JsonPropertyName("sensors")] public List<OpenAqSensor>? Sensors { get; set; }
        [JsonPropertyName("distance")] public double? Distance { get; set; }
    }

    private sealed class OpenAqCoordinates
    {
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
    }

    private sealed class OpenAqProvider
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private sealed class OpenAqSensor
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("parameter")] public OpenAqParameter? Parameter { get; set; }
    }

    private sealed class OpenAqParameter
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("units")] public string? Units { get; set; }
    }

    private sealed class OpenAqLatestResponse
    {
        [JsonPropertyName("results")] public List<OpenAqLatestResult>? Results { get; set; }
    }

    private sealed class OpenAqLatestResult
    {
        [JsonPropertyName("sensorsId")] public int SensorsId { get; set; }
        [JsonPropertyName("value")] public double Value { get; set; }
        [JsonPropertyName("datetime")] public OpenAqDateTime? Datetime { get; set; }
    }

    private sealed class OpenAqDateTime
    {
        [JsonPropertyName("utc")] public DateTime? Utc { get; set; }
        [JsonPropertyName("local")] public string? Local { get; set; }
    }
}
