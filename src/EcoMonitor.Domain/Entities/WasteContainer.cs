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
}
