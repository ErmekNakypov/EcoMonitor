using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Application.Common.Interfaces;

// Append-only audit logger for dumpsite reports. Each call writes one row to
// dumpsite_report_events. Caller passes the actor identity at event time;
// ActorDisplayName is a snapshot so the log stays readable even if the user
// is later renamed or deleted.
public interface IReportEventLogger
{
    Task LogAsync(
        Guid reportId,
        DumpsiteEventType eventType,
        Guid? actorUserId,
        string actorRole,
        string actorDisplayName,
        string? notes = null,
        object? payload = null,
        CancellationToken ct = default);
}
