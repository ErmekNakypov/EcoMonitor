using EcoMonitor.Domain.Enums;

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
}
