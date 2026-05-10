using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Application.Features.AirQuality.Dto;

public sealed record StationWithLatestReadingDto(
    Guid Id,
    string Name,
    string? Locality,
    string? ProviderName,
    double Latitude,
    double Longitude,
    double? Pm25,
    double? Pm10,
    double? Temperature,
    double? Humidity,
    double? Pressure,
    DateTime? MeasuredAt,
    AirQualitySource Source);

public sealed record StationDetailsDto(
    Guid Id,
    string ExternalId,
    string Name,
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
    DateTime? MeasuredAt);
