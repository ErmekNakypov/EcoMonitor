namespace EcoMonitor.Application.Features.Sensors.GetRecentFillReadings;

public sealed record FillReadingPointDto(
    DateTime MeasuredAt,
    double DistanceCm,
    double FillPercent,
    int? BatteryMv);
