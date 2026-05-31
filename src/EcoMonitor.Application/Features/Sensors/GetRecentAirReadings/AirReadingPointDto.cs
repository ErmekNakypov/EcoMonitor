namespace EcoMonitor.Application.Features.Sensors.GetRecentAirReadings;

public sealed record AirReadingPointDto(
    DateTime MeasuredAt,
    double? Pm25,
    double? Pm10,
    double? Temperature,
    double? Humidity,
    double? AqiUs);
