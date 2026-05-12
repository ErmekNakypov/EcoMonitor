using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

// Append-only audit record. One row per significant state change on a
// dumpsite report. Used to render the activity timeline on every Details
// page. Captures actor identity at the time of the event (ActorDisplayName
// is a snapshot — survives later renames or user deletion).
public class DumpsiteReportEvent : BaseEntity
{
    public Guid ReportId { get; set; }
    public DumpsiteReport Report { get; set; } = null!;

    public DumpsiteEventType EventType { get; set; }
    public DateTime OccurredAt { get; set; }

    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;       // "System", "Citizen", "Inspector", "CleanupCrew"
    public string ActorDisplayName { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public string? PayloadJson { get; set; }
}
