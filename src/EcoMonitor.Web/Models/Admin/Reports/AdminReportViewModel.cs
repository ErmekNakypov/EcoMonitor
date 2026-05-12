using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Reports;

namespace EcoMonitor.Web.Models.Admin.Reports;

public class AdminReportViewModel
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
    public DateTime? ReporterRegisteredAt { get; set; }
    public Guid? AssignedInspectorId { get; set; }
    public string? AssignedInspectorEmail { get; set; }
    public string? AssignedInspectorFullName { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ReportSource Source { get; set; }
    public string? TelegramUserName { get; set; }

    public DateTime? CleanupFlaggedAt { get; set; }
    public string? DistrictNameRu { get; set; }
    public string? DistrictColorHex { get; set; }

    public List<ReportEventViewModel> Events { get; set; } = new();
}
