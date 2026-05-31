using EcoMonitor.Domain.Common;

namespace EcoMonitor.Domain.Entities;

public class ContainerFillReading : BaseEntity
{
    public Guid ContainerId { get; set; }
    public Guid DeviceGuid { get; set; }
    public double DistanceCm { get; set; }
    public double FillPercent { get; set; }
    public int? BatteryMv { get; set; }
    public DateTime MeasuredAt { get; set; }
}
