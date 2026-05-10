using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

public enum IotDeviceStatus
{
    [Display(Name = "Active")]
    Active = 0,

    [Display(Name = "Suspended")]
    Suspended = 1,

    [Display(Name = "Decommissioned")]
    Decommissioned = 2
}
