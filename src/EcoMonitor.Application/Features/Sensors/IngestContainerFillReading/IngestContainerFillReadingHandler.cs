using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.Sensors.IngestContainerFillReading;

public class IngestContainerFillReadingHandler
    : IRequestHandler<IngestContainerFillReadingCommand, IngestContainerFillReadingResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ISensorRealtimePublisher _realtime;
    private readonly ILogger<IngestContainerFillReadingHandler> _logger;

    public IngestContainerFillReadingHandler(
        IApplicationDbContext db,
        ISensorRealtimePublisher realtime,
        ILogger<IngestContainerFillReadingHandler> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<IngestContainerFillReadingResult> Handle(
        IngestContainerFillReadingCommand request,
        CancellationToken cancellationToken)
    {
        var device = await _db.IotDevices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceGuid, cancellationToken);

        if (device is null || device.Status != IotDeviceStatus.Active)
        {
            return new IngestContainerFillReadingResult(false, null, null, "Device not found or not active");
        }

        if (device.ContainerId is null)
        {
            return new IngestContainerFillReadingResult(false, null, null, "Device is not assigned to any container");
        }

        var rangeError = Validate(request.Reading);
        if (rangeError is not null)
        {
            return new IngestContainerFillReadingResult(false, null, null, rangeError);
        }

        var container = await _db.WasteContainers
            .FirstOrDefaultAsync(c => c.Id == device.ContainerId.Value, cancellationToken);

        if (container is null)
        {
            return new IngestContainerFillReadingResult(false, null, null, "Assigned container no longer exists");
        }

        if (container.HeightCm <= 0)
        {
            return new IngestContainerFillReadingResult(
                false, null, null,
                "Container HeightCm is not configured — set the empty-bin sensor distance before ingesting readings");
        }

        var measuredAtUtc = ResolveMeasuredAtUtc(request.Reading.MeasuredAt);

        var fillPercent = ComputeFillPercent(container.HeightCm, request.Reading.DistanceCm);

        var reading = new ContainerFillReading
        {
            ContainerId = container.Id,
            DeviceGuid = device.Id,
            DistanceCm = request.Reading.DistanceCm,
            FillPercent = fillPercent,
            BatteryMv = request.Reading.BatteryMv,
            MeasuredAt = measuredAtUtc
        };
        _db.ContainerFillReadings.Add(reading);

        container.LastDistanceCm = request.Reading.DistanceCm;
        container.LastFillPercent = fillPercent;
        container.LastMeasuredAt = measuredAtUtc;

        device.LastSeenAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Container fill reading {ReadingId} ingested from device {DeviceId} (container {ContainerId}, {FillPercent:F1}%)",
            reading.Id, device.DeviceId, container.Id, fillPercent);

        await _realtime.PublishFillReadingAsync(
            container.Id,
            measuredAtUtc,
            request.Reading.DistanceCm,
            fillPercent,
            cancellationToken);

        return new IngestContainerFillReadingResult(true, reading.Id, fillPercent, null);
    }

    private static DateTime ResolveMeasuredAtUtc(DateTime? measuredAt)
    {
        if (measuredAt is null)
        {
            return DateTime.UtcNow;
        }
        return measuredAt.Value.Kind == DateTimeKind.Utc
            ? measuredAt.Value
            : measuredAt.Value.ToUniversalTime();
    }

    private static double ComputeFillPercent(double heightCm, double distanceCm)
    {
        var pct = (heightCm - distanceCm) / heightCm * 100.0;
        if (pct < 0) return 0;
        if (pct > 100) return 100;
        return pct;
    }

    private static string? Validate(ContainerFillReadingDto r)
    {
        if (double.IsNaN(r.DistanceCm) || double.IsInfinity(r.DistanceCm)) return "DistanceCm is not a finite number";
        if (r.DistanceCm < 0) return "DistanceCm must be >= 0";
        if (r.DistanceCm > 1000) return "DistanceCm out of range [0, 1000]";
        if (r.BatteryMv is { } mv && (mv < 0 || mv > 20000)) return "BatteryMv out of range [0, 20000]";
        return null;
    }
}
