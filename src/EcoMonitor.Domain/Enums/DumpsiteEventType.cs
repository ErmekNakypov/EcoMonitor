namespace EcoMonitor.Domain.Enums;

// Numeric values are frozen — they live in dumpsite_report_events.event_type.
// Append new event types at the end; never reorder.
public enum DumpsiteEventType
{
    ReportSubmitted = 0,
    AutoTriaged = 1,
    SentToReview = 2,
    InspectorTook = 3,
    Confirmed = 4,
    Rejected = 5,
    CleanupTaken = 6,
    CleanupStarted = 7,
    CleanupCompleted = 8,
    MarkedResolved = 9,
    Appealed = 10,
    AppealUpheld = 11,
    AppealDismissed = 12,
    ReworkStarted = 13,
    AutoClosed = 14,
    CleanupFlagged = 15,
    ReassignedToAnotherCrew = 16
}
