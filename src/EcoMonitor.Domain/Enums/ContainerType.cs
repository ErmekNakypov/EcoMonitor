using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

public enum ContainerType
{
    [Display(Name = "General waste")]
    General,

    [Display(Name = "Plastic")]
    Plastic,

    [Display(Name = "Glass")]
    Glass,

    [Display(Name = "Paper")]
    Paper,

    [Display(Name = "Organic")]
    Organic
}
