using System.ComponentModel.DataAnnotations;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Devices;

public class CreateDeviceViewModel
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(42.5, 43.2)]
    public double Latitude { get; set; } = 42.876;

    [Range(74.3, 74.9)]
    public double Longitude { get; set; } = 74.604;
}

public class EditDeviceViewModel
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(42.5, 43.2)]
    public double Latitude { get; set; }

    [Range(74.3, 74.9)]
    public double Longitude { get; set; }

    public IotDeviceStatus Status { get; set; }
}

public class TokenIssuedViewModel
{
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
