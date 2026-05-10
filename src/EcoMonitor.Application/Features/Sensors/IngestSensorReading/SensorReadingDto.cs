namespace EcoMonitor.Application.Features.Sensors.IngestSensorReading;

public sealed record SensorReadingDto(
    DateTime MeasuredAt,
    double? Pm25,
    double? Pm10,
    double? Temperature,
    double? Humidity,
    double? Pressure);
