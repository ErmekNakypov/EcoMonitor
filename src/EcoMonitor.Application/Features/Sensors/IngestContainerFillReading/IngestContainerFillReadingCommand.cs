using MediatR;

namespace EcoMonitor.Application.Features.Sensors.IngestContainerFillReading;

public sealed record IngestContainerFillReadingCommand(Guid DeviceGuid, ContainerFillReadingDto Reading)
    : IRequest<IngestContainerFillReadingResult>;

public sealed record IngestContainerFillReadingResult(
    bool Success,
    Guid? ReadingId,
    double? FillPercent,
    string? Error);
