using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Reports;

public class AdminReportViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DumpsiteStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public IReadOnlyList<string> PhotoPaths { get; set; } = Array.Empty<string>();
    public Guid ReporterId { get; set; }
    public string ReporterEmail { get; set; } = string.Empty;
    public string ReporterFullName { get; set; } = string.Empty;
    public DateTime ReporterRegisteredAt { get; set; }
    public Guid? AssignedInspectorId { get; set; }
    public string? AssignedInspectorEmail { get; set; }
    public string? AssignedInspectorFullName { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
