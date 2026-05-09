using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

public enum ContainerStatus
{
    [Display(Name = "Active")]
    Active,

    [Display(Name = "Under maintenance")]
    Maintenance,

    [Display(Name = "Removed")]
    Removed
}
