using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class WasteContainer : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public ContainerType Type { get; set; }
    public int Capacity { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.Active;
    public DateTime InstalledAt { get; set; }
    public long? OsmId { get; set; }
    public bool IsImported { get; set; }

    // Distance from the ultrasonic sensor (mounted on the lid) to the
    // empty-bin floor. Required to translate raw HC-SR04 distance readings
    // into a fill percentage. 0 means "not configured" — ingestion will
    // reject readings until an admin sets a real height.
    public double HeightCm { get; set; }

    public double? LastFillPercent { get; set; }
    public double? LastDistanceCm { get; set; }
    public DateTime? LastMeasuredAt { get; set; }

    // Resolved from (Latitude, Longitude) by IDistrictResolver at create /
    // import time so a "container is full" cleanup task auto-routes to the
    // right district inspector. Nullable: containers outside every district
    // polygon legitimately stay null and fall back to the broadcast path.
    public Guid? DistrictId { get; set; }
}
