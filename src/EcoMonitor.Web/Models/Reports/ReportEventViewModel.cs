using EcoMonitor.Application.Features.DumpsiteReports.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Reports;

public class ReportEventViewModel
{
    public DateTime OccurredAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string IconName { get; set; } = "circle";
    public string MarkerClass { get; set; } = "neutral";
}

public static class ReportEventViewModelMapper
{
    // Citizens don't see internal handoffs that don't affect them.
    private static readonly HashSet<DumpsiteEventType> CitizenHidden = new()
    {
        DumpsiteEventType.InspectorTook,
        DumpsiteEventType.CleanupTaken,
        DumpsiteEventType.CleanupFlagged,
        DumpsiteEventType.ReassignedToAnotherCrew
    };

    public static ReportEventViewModel Map(ReportEventDto e)
    {
        var (title, icon, marker) = e.EventType switch
        {
            DumpsiteEventType.ReportSubmitted   => ("Report submitted",                       "file-earmark-plus",        "neutral"),
            DumpsiteEventType.AutoTriaged       => ("Auto-confirmed by triage system",        "lightning-charge-fill",    "success"),
            DumpsiteEventType.SentToReview      => ("Flagged for inspector review",           "exclamation-triangle-fill","warning"),
            DumpsiteEventType.InspectorTook     => ("Taken for review",                       "person-check-fill",        "info"),
            DumpsiteEventType.Confirmed         => ("Confirmed by inspector",                 "check-circle-fill",        "success"),
            DumpsiteEventType.Rejected          => ("Rejected",                               "x-circle-fill",            "danger"),
            DumpsiteEventType.CleanupTaken      => ("Cleanup task accepted",                  "hand-thumbs-up-fill",      "info"),
            DumpsiteEventType.CleanupStarted    => ("Cleanup started",                        "tools",                    "info"),
            DumpsiteEventType.CleanupCompleted  => ("Cleanup completed",                      "check2-square",            "success"),
            DumpsiteEventType.MarkedResolved    => ("Marked as resolved",                     "shield-check",             "success"),
            DumpsiteEventType.Appealed          => ("Appealed by citizen",                    "flag-fill",                "warning"),
            DumpsiteEventType.AppealUpheld      => ("Appeal upheld",                          "check-circle-fill",        "warning"),
            DumpsiteEventType.AppealDismissed   => ("Appeal dismissed",                       "x-circle-fill",            "neutral"),
            DumpsiteEventType.ReworkStarted     => ("Rework started",                         "arrow-counterclockwise",   "warning"),
            DumpsiteEventType.AutoClosed        => ("Auto-closed (appeal window passed)",     "lock-fill",                "neutral"),
            DumpsiteEventType.CleanupFlagged    => ("Cleanup crew flagged",                   "flag-fill",                "warning"),
            DumpsiteEventType.ReassignedToAnotherCrew => ("Reassigned to another crew",       "arrow-left-right",         "info"),
            _                                    => ("Event",                                  "circle",                   "neutral")
        };

        return new ReportEventViewModel
        {
            OccurredAt = e.OccurredAt,
            Title = title,
            ActorDisplayName = e.ActorDisplayName,
            Notes = e.Notes,
            IconName = icon,
            MarkerClass = marker
        };
    }

    public static List<ReportEventViewModel> MapStaff(IEnumerable<ReportEventDto> events)
        => events.OrderBy(e => e.OccurredAt).Select(Map).ToList();

    public static List<ReportEventViewModel> MapCitizen(IEnumerable<ReportEventDto> events)
        => events.Where(e => !CitizenHidden.Contains(e.EventType))
                 .OrderBy(e => e.OccurredAt)
                 .Select(Map)
                 .ToList();
}
