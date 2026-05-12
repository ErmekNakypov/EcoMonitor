using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Reports;

namespace EcoMonitor.Web.Models.Inspector;

public class ReportViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DumpsiteStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public IReadOnlyList<string> PhotoPaths { get; set; } = Array.Empty<string>();
    public Guid? ReporterId { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterFullName { get; set; }
    public Guid? AssignedInspectorId { get; set; }
    public string? AssignedInspectorEmail { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ReportSource Source { get; set; }
    public string? TelegramUserName { get; set; }

    public string? InspectorObservations { get; set; }
    public IReadOnlyList<string> InspectionPhotos { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> CleanupBeforePhotos { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> CleanupAfterPhotos { get; set; } = Array.Empty<string>();
    public Guid? CleanupCrewId { get; set; }
    public string? CleanupCrewName { get; set; }
    public DateTime? CleanupStartedAt { get; set; }
    public DateTime? CleanupCompletedAt { get; set; }
    public string? CleanupNotes { get; set; }
    public string? AutoTriageReason { get; set; }

    public long? TelegramUserId { get; set; }
    public int ReporterTotalReports { get; set; }
    public int ReporterPendingReports { get; set; }
    public int ReporterResolvedReports { get; set; }
    public int ReporterRejectedReports { get; set; }

    public bool IsAssignedToCurrentUser { get; set; }
    public bool CanTake => Status == DumpsiteStatus.New && AssignedInspectorId is null;
    public bool CanConfirm => Status == DumpsiteStatus.InReview && IsAssignedToCurrentUser;
    public bool CanReject => Status == DumpsiteStatus.InReview && IsAssignedToCurrentUser;
    // Verification step: any inspector can verify, not just the original assignee.
    public bool CanVerify => Status == DumpsiteStatus.AwaitingVerification;

    public DateTime? AppealedAt { get; set; }
    public string? AppealReason { get; set; }
    public IReadOnlyList<string> AppealPhotos { get; set; } = Array.Empty<string>();
    public DateTime? AppealReviewedAt { get; set; }
    public string? AppealResolutionNotes { get; set; }
    public AppealOutcome? AppealOutcome { get; set; }
    public DateTime? ClosedAt { get; set; }

    public bool CanReviewAppeal => Status == DumpsiteStatus.Appealed;

    public string? CleanupRejectionReason { get; set; }
    public string? CleanupRejectionNotes { get; set; }
    public DateTime? CleanupFlaggedAt { get; set; }
    public string? CleanupFlaggedByCrewName { get; set; }
    public int ReassignCount { get; set; }
    public IReadOnlyList<string> FlagEvidencePhotos { get; set; } = Array.Empty<string>();

    public bool CanReviewFlag => Status == DumpsiteStatus.FlaggedByCleanupCrew;

    public Guid? DistrictId { get; set; }
    public string? DistrictNameRu { get; set; }
    public string? DistrictNameEn { get; set; }
    public string? DistrictColorHex { get; set; }
    public string? DistrictAssignedInspectorName { get; set; }

    public List<ReportEventViewModel> Events { get; set; } = new();
}
