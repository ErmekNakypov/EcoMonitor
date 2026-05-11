using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class DumpsiteCleanupPhoto : BaseEntity
{
    public Guid ReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public CleanupPhotoType Type { get; set; }
    public Guid UploadedByUserId { get; set; }
}
