using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Citizen;

public class ReportDetailsViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DumpsiteStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public IReadOnlyList<string> PhotoPaths { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> InspectionPhotos { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> BeforeCleanupPhotos { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AfterCleanupPhotos { get; set; } = Array.Empty<string>();
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DateTime? AppealedAt { get; set; }
    public string? AppealReason { get; set; }
    public IReadOnlyList<string> AppealPhotos { get; set; } = Array.Empty<string>();
    public DateTime? AppealReviewedAt { get; set; }
    public string? AppealResolutionNotes { get; set; }
    public AppealOutcome? AppealOutcome { get; set; }
    public DateTime? ClosedAt { get; set; }

    public string? CleanupCrewName { get; set; }
    public DateTime? CleanupCompletedAt { get; set; }
    public string? VerifiedByInspectorName { get; set; }

    // Resolution notes also carry the rejection reason when Status == Rejected.
    public string? RejectionReason => Status == DumpsiteStatus.Rejected ? ResolutionNotes : null;

    public bool CanAppeal =>
        Status == DumpsiteStatus.Resolved
        && ResolvedAt is not null
        && DateTime.UtcNow - ResolvedAt.Value < TimeSpan.FromDays(7);

    public TimeSpan? AppealTimeRemaining =>
        Status == DumpsiteStatus.Resolved && ResolvedAt is not null
            ? TimeSpan.FromDays(7) - (DateTime.UtcNow - ResolvedAt.Value)
            : null;
}
