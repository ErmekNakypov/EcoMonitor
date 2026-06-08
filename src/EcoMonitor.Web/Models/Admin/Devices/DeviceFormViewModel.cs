using System.ComponentModel.DataAnnotations;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Devices;

// [Display(Name)] values are resource KEYS routed through
// DataAnnotationLocalizerProvider to per-VM bundles under
// Resources/Models/Admin/Devices/*.{culture}.resx.
public class CreateDeviceViewModel
{
    [Required, StringLength(200, MinimumLength = 3)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(42.5, 43.2)]
    [Display(Name = "Latitude")]
    public double Latitude { get; set; } = 42.876;

    [Range(74.3, 74.9)]
    [Display(Name = "Longitude")]
    public double Longitude { get; set; } = 74.604;
}

public class EditDeviceViewModel
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(42.5, 43.2)]
    [Display(Name = "Latitude")]
    public double Latitude { get; set; }

    [Range(74.3, 74.9)]
    [Display(Name = "Longitude")]
    public double Longitude { get; set; }

    [Display(Name = "Status")]
    public IotDeviceStatus Status { get; set; }
}

public class TokenIssuedViewModel
{
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
