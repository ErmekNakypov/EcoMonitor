using System.ComponentModel.DataAnnotations;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Containers;

public class EditContainerViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [Required]
    [Display(Name = "Type")]
    public ContainerType Type { get; set; }

    [Range(1, 10000)]
    [Display(Name = "Capacity")]
    public int Capacity { get; set; }

    [Required]
    [Display(Name = "Status")]
    public ContainerStatus Status { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Installed at")]
    public DateTime InstalledAt { get; set; }
}
