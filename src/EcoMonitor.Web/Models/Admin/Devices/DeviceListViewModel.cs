using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Devices;

public class DeviceListViewModel
{
    public IReadOnlyList<DeviceRowViewModel> Items { get; set; } = Array.Empty<DeviceRowViewModel>();
    public int TotalCount { get; set; }
}

public class DeviceRowViewModel
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public IotDeviceStatus Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsOffline =>
        Status == IotDeviceStatus.Active
        && (LastSeenAt is null || LastSeenAt < DateTime.UtcNow.AddMinutes(-30));
}
