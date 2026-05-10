using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class IqAirAirQualityProvider : IAirQualityProvider
{
    public string Name => "IqAir";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IqAirAirQualityProvider> _logger;

    private static readonly (string Probe, double Lat, double Lng)[] CityProbes =
    {
        ("Bishkek",    42.8746, 74.5698),
        ("Osh",        40.5283, 72.7985),
        ("Karakol",    42.4907, 78.3936),
        ("Jalal-Abad", 40.9333, 73.0000),
        ("Talas",      42.5197, 72.2422),
        ("Naryn",      41.4287, 75.9911),
    };

    public IqAirAirQualityProvider(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<IqAirAirQualityProvider> logger)
    {
        _httpFactory = httpFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderStationReading>> FetchCurrentReadingsAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration["IqAir:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("IQAir API key is not configured. Set IqAir:ApiKey in user secrets.");
            return Array.Empty<ProviderStationReading>();
        }

        var client = _httpFactory.CreateClient("iqair");
        var readings = new List<ProviderStationReading>();
        var freshnessCutoff = DateTime.UtcNow - TimeSpan.FromDays(7);

        foreach (var (probe, lat, lng) in CityProbes)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var url = $"v2/nearest_city?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lng.ToString(CultureInfo.InvariantCulture)}&key={apiKey}";
                var resp = await client.GetFromJsonAsync<IqAirNearestCityResponse>(url, ct);

                if (resp?.Status != "success" || resp.Data is null)
                {
                    _logger.LogInformation("IQAir: no data for probe {Probe}", probe);
                    continue;
                }

                var d = resp.Data;
                var pollution = d.Current?.Pollution;
                var weather = d.Current?.Weather;

                if (pollution?.Ts is null) continue;
                if (pollution.Ts.Value < freshnessCutoff) continue;

                double cityLat = lat, cityLng = lng;
                if (d.Location?.Coordinates is { Length: 2 } coords)
                {
                    cityLng = coords[0];
                    cityLat = coords[1];
                }

                var externalId = $"{(d.City ?? probe).Replace(' ', '_')}__{(d.State ?? string.Empty).Replace(' ', '_')}";

                readings.Add(new ProviderStationReading(
                    ExternalId: externalId,
                    StationName: $"{d.City} ({d.State})",
                    Locality: d.City,
                    ProviderName: "IQAir AirVisual",
                    Latitude: cityLat,
                    Longitude: cityLng,
                    Source: AirQualitySource.External,
                    Pm25: null,
                    Pm10: null,
                    Temperature: weather?.Temperature,
                    Humidity: weather?.Humidity,
                    Pressure: weather?.Pressure,
                    AqiUs: pollution.AqiUs,
                    MeasuredAt: pollution.Ts.Value));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "IQAir request failed for probe {Probe}", probe);
            }

            try
            {
                await Task.Delay(200, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "IQAir: queried {Probes} cities, got {Fresh} fresh readings",
            CityProbes.Length, readings.Count);

        return readings;
    }

    private sealed class IqAirNearestCityResponse
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("data")] public IqAirCityData? Data { get; set; }
    }

    private sealed class IqAirCityData
    {
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("location")] public IqAirLocation? Location { get; set; }
        [JsonPropertyName("current")] public IqAirCurrent? Current { get; set; }
    }

    private sealed class IqAirLocation
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("coordinates")] public double[]? Coordinates { get; set; }
    }

    private sealed class IqAirCurrent
    {
        [JsonPropertyName("pollution")] public IqAirPollution? Pollution { get; set; }
        [JsonPropertyName("weather")] public IqAirWeather? Weather { get; set; }
    }

    private sealed class IqAirPollution
    {
        [JsonPropertyName("ts")] public DateTime? Ts { get; set; }
        [JsonPropertyName("aqius")] public double? AqiUs { get; set; }
        [JsonPropertyName("mainus")] public string? MainUs { get; set; }
        [JsonPropertyName("aqicn")] public double? AqiCn { get; set; }
        [JsonPropertyName("maincn")] public string? MainCn { get; set; }
    }

    private sealed class IqAirWeather
    {
        [JsonPropertyName("ts")] public DateTime? Ts { get; set; }
        [JsonPropertyName("tp")] public double? Temperature { get; set; }
        [JsonPropertyName("hu")] public double? Humidity { get; set; }
        [JsonPropertyName("pr")] public double? Pressure { get; set; }
        [JsonPropertyName("ws")] public double? WindSpeed { get; set; }
        [JsonPropertyName("wd")] public double? WindDirection { get; set; }
    }
}
