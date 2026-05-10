using MediatR;

namespace EcoMonitor.Application.Features.Sensors.IngestSensorReading;

public sealed record IngestSensorReadingCommand(Guid DeviceGuid, SensorReadingDto Reading)
    : IRequest<IngestSensorReadingResult>;

public sealed record IngestSensorReadingResult(bool Success, Guid? ReadingId, string? Error);
