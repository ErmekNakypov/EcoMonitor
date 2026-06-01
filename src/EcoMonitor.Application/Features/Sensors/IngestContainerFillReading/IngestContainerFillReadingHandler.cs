using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Common.Services.Sensors;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.Sensors.IngestContainerFillReading;

public class IngestContainerFillReadingHandler
    : IRequestHandler<IngestContainerFillReadingCommand, IngestContainerFillReadingResult>
{
    // Statuses for the dedup check below: a report in any of these is NOT
    // open / not actively in someone's queue, so a fresh fill crossing is
    // allowed to spawn a new IoT task.
    private static readonly DumpsiteStatus[] TerminalStatuses =
    {
        DumpsiteStatus.Resolved,
        DumpsiteStatus.Rejected,
        DumpsiteStatus.Closed
    };

    private readonly IApplicationDbContext _db;
    private readonly ISensorRealtimePublisher _realtime;
    private readonly IMediator _mediator;
    private readonly ILogger<IngestContainerFillReadingHandler> _logger;

    public IngestContainerFillReadingHandler(
        IApplicationDbContext db,
        ISensorRealtimePublisher realtime,
        IMediator mediator,
        ILogger<IngestContainerFillReadingHandler> logger)
    {
        _db = db;
        _realtime = realtime;
        _mediator = mediator;
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

        // Capture the prior snapshot BEFORE we overwrite it — the edge-
        // trigger below compares "what we saw last" to "what we see now".
        var previousFillPercent = container.LastFillPercent;

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

        // ----- Edge-triggered cleanup-task creation -----
        // Trigger ONCE per fill cycle: the previous reading was below the
        // FULL threshold AND this reading reached it. A bin that stays full
        // (next reading also >= 90%) will NOT spawn a duplicate task here.
        //
        // A SECOND defence at the data layer: even if an edge somehow looks
        // like a fresh crossing (e.g. process restart, sensor reset, the
        // previous LastFillPercent was null), refuse to create a new task
        // if an open IoT-sourced report already exists for this container.
        // We match by exact coordinates because we copy them from the
        // container into the report, so it's a reliable proxy without
        // needing a ContainerId FK on DumpsiteReport.
        //
        // Wrapped in try/catch so a failure here NEVER fails the sensor
        // POST — telemetry ingest stays resilient, same convention as the
        // SignalR publisher above.
        var previous = previousFillPercent ?? 0.0;
        var crossedUp = previous < FillThresholds.FullPercent
                     && fillPercent >= FillThresholds.FullPercent;
        if (crossedUp)
        {
            try
            {
                await TryCreateIotCleanupTaskAsync(container, fillPercent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to create IoT cleanup task for container {ContainerId} after fill={FillPercent:F1}%",
                    container.Id, fillPercent);
            }
        }

        return new IngestContainerFillReadingResult(true, reading.Id, fillPercent, null);
    }

    private async Task TryCreateIotCleanupTaskAsync(
        WasteContainer container,
        double fillPercent,
        CancellationToken ct)
    {
        // Dedup: an OPEN IoT-sourced report already covering this same
        // container (matched by exact lat/lng, since we always copy them
        // from the container) means a task is already in someone's queue.
        // Skip; whoever owns that task will resolve the physical fill.
        var alreadyOpen = await _db.DumpsiteReports
            .AsNoTracking()
            .AnyAsync(r => r.Source == ReportSource.Iot
                        && r.Latitude == container.Latitude
                        && r.Longitude == container.Longitude
                        && !TerminalStatuses.Contains(r.Status), ct);

        if (alreadyOpen)
        {
            _logger.LogInformation(
                "IoT task suppressed for container {ContainerId} — an open IoT report already exists",
                container.Id);
            return;
        }

        // Description format chosen for the inspector's queue: container
        // code first (operationally identifiable), then the measured fill,
        // then the registered address.
        var description =
            $"Waste container {container.Code} is full ({fillPercent:F0}%). " +
            $"Location: {container.Address}";

        await _mediator.Send(new SubmitDumpsiteReportCommand(
            ReporterId: null,
            Description: description,
            Latitude: container.Latitude,
            Longitude: container.Longitude,
            Photos: Array.Empty<UploadedPhotoDto>(),
            Source: ReportSource.Iot), ct);

        _logger.LogInformation(
            "IoT cleanup task created for container {ContainerId} (fill={FillPercent:F1}%)",
            container.Id, fillPercent);
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
