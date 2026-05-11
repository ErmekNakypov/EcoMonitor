using EcoMonitor.Domain.Common;

namespace EcoMonitor.Domain.Entities;

public class DumpsiteInspectionPhoto : BaseEntity
{
    public Guid ReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public Guid UploadedByInspectorId { get; set; }
    public DateTime UploadedAt { get; set; }
}
