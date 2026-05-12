using EcoMonitor.Domain.Common;

namespace EcoMonitor.Domain.Entities;

public class DumpsiteAppealPhoto : BaseEntity
{
    public Guid ReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public Guid UploadedByCitizenId { get; set; }
    public DateTime UploadedAt { get; set; }
}
