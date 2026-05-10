using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IAirQualityProvider
{
    string Name { get; }
    Task<IReadOnlyList<ProviderStationReading>> FetchCurrentReadingsAsync(CancellationToken ct = default);
}

public sealed record ProviderStationReading(
    string ExternalId,
    string StationName,
    string? Locality,
    string? ProviderName,
    double Latitude,
    double Longitude,
    AirQualitySource Source,
    double? Pm25,
    double? Pm10,
    double? Temperature,
    double? Humidity,
    double? Pressure,
    DateTime MeasuredAt);
