using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Containers;

public class ContainerListItemViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ContainerType Type { get; set; }
    public ContainerStatus Status { get; set; }
    public int Capacity { get; set; }
    public DateTime InstalledAt { get; set; }
}
