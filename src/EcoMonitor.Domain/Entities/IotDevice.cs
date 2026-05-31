using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class IotDevice : BaseEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public IotDeviceStatus Status { get; set; } = IotDeviceStatus.Active;
    public DateTime? LastSeenAt { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime TokenIssuedAt { get; set; }

    // Optional link to the waste container this device is mounted on.
    // Air-quality devices leave this null; HC-SR04 fill-level devices set
    // it during provisioning and use it to route readings.
    public Guid? ContainerId { get; set; }
}
