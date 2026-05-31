namespace EcoMonitor.Application.Features.Sensors.GetContainerLatestFill;

public sealed record ContainerLatestFillDto(
    Guid ContainerId,
    string Code,
    double HeightCm,
    double? LastFillPercent,
    double? LastDistanceCm,
    DateTime? LastMeasuredAt);
