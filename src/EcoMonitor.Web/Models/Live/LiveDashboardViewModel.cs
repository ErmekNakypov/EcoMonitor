namespace EcoMonitor.Web.Models.Live;

public sealed class LiveDashboardViewModel
{
    public Guid AirStationId { get; init; }
    public string AirDeviceName { get; init; } = string.Empty;
    public Guid ContainerId { get; init; }
    public string ContainerCode { get; init; } = string.Empty;
}
