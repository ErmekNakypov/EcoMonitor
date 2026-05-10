using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class AirQualityStation : BaseEntity
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Locality { get; set; }
    public string? ProviderName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public AirQualitySource Source { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastReadingAt { get; set; }
}
