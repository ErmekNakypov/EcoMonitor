using EcoMonitor.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EcoMonitor.Web.Hubs;

public sealed class SignalRSensorRealtimePublisher : ISensorRealtimePublisher
{
    private readonly IHubContext<SensorHub> _hub;
    private readonly ILogger<SignalRSensorRealtimePublisher> _logger;

    public SignalRSensorRealtimePublisher(
        IHubContext<SensorHub> hub,
        ILogger<SignalRSensorRealtimePublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishAirReadingAsync(
        Guid stationId,
        DateTime measuredAt,
        double? pm25,
        double? pm10,
        double? temperature,
        double? humidity,
        double? aqiUs,
        CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients.All.SendAsync(
                "airReading",
                new
                {
                    stationId,
                    measuredAt,
                    pm25,
                    pm10,
                    temperature,
                    humidity,
                    aqiUs
                },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast airReading for station {StationId}", stationId);
        }
    }

    public async Task PublishFillReadingAsync(
        Guid containerId,
        DateTime measuredAt,
        double distanceCm,
        double fillPercent,
        CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients.All.SendAsync(
                "fillReading",
                new
                {
                    containerId,
                    measuredAt,
                    distanceCm,
                    fillPercent
                },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast fillReading for container {ContainerId}", containerId);
        }
    }
}
