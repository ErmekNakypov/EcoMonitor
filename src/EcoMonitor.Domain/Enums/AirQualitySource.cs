using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

public enum AirQualitySource
{
    [Display(Name = "Own sensor")]
    OwnSensor,

    [Display(Name = "External provider")]
    External
}
