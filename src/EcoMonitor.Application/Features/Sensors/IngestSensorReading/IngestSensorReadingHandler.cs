using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Utils;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.Sensors.IngestSensorReading;

public class IngestSensorReadingHandler : IRequestHandler<IngestSensorReadingCommand, IngestSensorReadingResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ISensorRealtimePublisher _realtime;
    private readonly ILogger<IngestSensorReadingHandler> _logger;

    public IngestSensorReadingHandler(
        IApplicationDbContext db,
        ISensorRealtimePublisher realtime,
        ILogger<IngestSensorReadingHandler> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<IngestSensorReadingResult> Handle(IngestSensorReadingCommand request, CancellationToken cancellationToken)
    {
        var device = await _db.IotDevices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceGuid, cancellationToken);

        if (device is null || device.Status != IotDeviceStatus.Active)
        {
            return new IngestSensorReadingResult(false, null, "Device not found or not active");
        }

        var validation = Validate(request.Reading);
        if (validation is not null)
        {
            return new IngestSensorReadingResult(false, null, validation);
        }

        var measuredAtUtc = ResolveMeasuredAtUtc(request.Reading.MeasuredAt);

        var station = await _db.AirQualityStations
            .FirstOrDefaultAsync(
                s => s.ExternalId == device.DeviceId && s.Source == AirQualitySource.OwnSensor,
                cancellationToken);

        if (station is null)
        {
            station = new AirQualityStation
            {
                ExternalId = device.DeviceId,
                Name = device.Name,
                Locality = "Bishkek",
                ProviderName = "Own sensor",
                Latitude = device.Latitude,
                Longitude = device.Longitude,
                Source = AirQualitySource.OwnSensor,
                IsActive = true
            };
            _db.AirQualityStations.Add(station);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var existing = await _db.AirQualityReadings
            .FirstOrDefaultAsync(
                r => r.StationId == station.Id && r.MeasuredAt == measuredAtUtc,
                cancellationToken);

        if (existing is not null)
        {
            device.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Duplicate reading skipped for device {DeviceId} at {MeasuredAt}",
                device.DeviceId, measuredAtUtc);
            return new IngestSensorReadingResult(true, existing.Id, null);
        }

        var reading = new AirQualityReading
        {
            StationId = station.Id,
            Latitude = station.Latitude,
            Longitude = station.Longitude,
            Pm25 = request.Reading.Pm25,
            Pm10 = request.Reading.Pm10,
            Temperature = request.Reading.Temperature,
            Humidity = request.Reading.Humidity,
            Pressure = request.Reading.Pressure,
            AqiUs = EpaAqiCalculator.Pm25ToAqi(request.Reading.Pm25),
            MeasuredAt = measuredAtUtc,
            Source = AirQualitySource.OwnSensor
        };
        _db.AirQualityReadings.Add(reading);

        station.LastReadingAt = measuredAtUtc;
        device.LastSeenAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sensor reading {ReadingId} ingested from device {DeviceId} (station {StationId})",
            reading.Id, device.DeviceId, station.Id);

        await _realtime.PublishAirReadingAsync(
            station.Id,
            measuredAtUtc,
            reading.Pm25,
            reading.Pm10,
            reading.Temperature,
            reading.Humidity,
            reading.AqiUs,
            cancellationToken);

        return new IngestSensorReadingResult(true, reading.Id, null);
    }

    // Firmware can submit a default/uninitialised timestamp (DateTime.MinValue or a
    // pre-2000 epoch when its RTC hasn't synced yet). Treat those as "missing" and
    // stamp UtcNow rather than letting -infinity / Year 1970 rows poison the chart.
    private static DateTime ResolveMeasuredAtUtc(DateTime measuredAt)
    {
        if (measuredAt == default || measuredAt.Year < 2000)
        {
            return DateTime.UtcNow;
        }
        return measuredAt.Kind == DateTimeKind.Utc
            ? measuredAt
            : measuredAt.ToUniversalTime();
    }

    private static string? Validate(SensorReadingDto r)
    {
        if (r.Pm25 is null && r.Pm10 is null)
        {
            return "Reading must include at least Pm25 or Pm10";
        }
        if (r.Pm25 is { } pm25 && (pm25 < 0 || pm25 > 1000)) return "Pm25 out of range [0, 1000]";
        if (r.Pm10 is { } pm10 && (pm10 < 0 || pm10 > 2000)) return "Pm10 out of range [0, 2000]";
        if (r.Temperature is { } temp && (temp < -50 || temp > 60)) return "Temperature out of range [-50, 60]";
        if (r.Humidity is { } hum && (hum < 0 || hum > 100)) return "Humidity out of range [0, 100]";
        if (r.Pressure is { } pres && (pres < 800 || pres > 1200)) return "Pressure out of range [800, 1200]";
        return null;
    }
}
