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

    // Inspector confirmation
    public string? InspectorObservations { get; set; }

    // Cleanup crew
    public Guid? CleanupCrewId { get; set; }
    public DateTime? CleanupStartedAt { get; set; }
    public DateTime? CleanupCompletedAt { get; set; }
    public string? CleanupNotes { get; set; }

    // Final verification by (possibly different) inspector
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByInspectorId { get; set; }
}
