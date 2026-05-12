using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class DumpsiteReport : BaseEntity
{
    public Guid? ReporterId { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DumpsiteStatus Status { get; set; } = DumpsiteStatus.New;
    public List<string> PhotoPaths { get; set; } = new();
    public Guid? AssignedInspectorId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ReportSource Source { get; set; } = ReportSource.Web;
    public long? TelegramUserId { get; set; }
    public string? TelegramUserName { get; set; }

    // Auto-triage: null = passed all rules and was auto-confirmed; otherwise
    // a human-readable explanation of why the report was routed to InReview.
    public string? AutoTriageReason { get; set; }

    // Inspector confirmation
    public DateTime? ConfirmedAt { get; set; }
    public string? InspectorObservations { get; set; }

    // Cleanup crew
    public Guid? CleanupCrewId { get; set; }
    public DateTime? CleanupStartedAt { get; set; }
    public DateTime? CleanupCompletedAt { get; set; }
    public string? CleanupNotes { get; set; }

    // Rework after upheld appeal — incremented each time an appeal is upheld
    // and the report goes back to CleanupInProgress.
    public int ReworkCount { get; set; }
    public DateTime? ReworkStartedAt { get; set; }

    // Final verification by (possibly different) inspector
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByInspectorId { get; set; }

    // Citizen appeal of a Resolved report (7-day window).
    public DateTime? AppealedAt { get; set; }
    public string? AppealReason { get; set; }
    public DateTime? AppealReviewedAt { get; set; }
    public Guid? AppealReviewedByInspectorId { get; set; }
    public string? AppealResolutionNotes { get; set; }
    public AppealOutcome? AppealOutcome { get; set; }
    public DateTime? ClosedAt { get; set; }
}
