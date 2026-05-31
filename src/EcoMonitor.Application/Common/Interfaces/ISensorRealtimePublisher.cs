namespace EcoMonitor.Application.Common.Interfaces;

// Application-layer abstraction over the live-dashboard transport (SignalR in Web).
// Implementations MUST NOT throw — telemetry push is best-effort and must never
// fail an ingest. Callers do not need to wrap calls in try/catch.
public interface ISensorRealtimePublisher
{
    Task PublishAirReadingAsync(
        Guid stationId,
        DateTime measuredAt,
        double? pm25,
        double? pm10,
        double? temperature,
        double? humidity,
        double? aqiUs,
        CancellationToken ct = default);

    Task PublishFillReadingAsync(
        Guid containerId,
        DateTime measuredAt,
        double distanceCm,
        double fillPercent,
        CancellationToken ct = default);
}
