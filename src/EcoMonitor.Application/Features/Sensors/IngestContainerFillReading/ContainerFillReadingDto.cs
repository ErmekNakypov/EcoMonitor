namespace EcoMonitor.Application.Features.Sensors.IngestContainerFillReading;

public sealed record ContainerFillReadingDto(
    double DistanceCm,
    int? BatteryMv,
    DateTime? MeasuredAt);
