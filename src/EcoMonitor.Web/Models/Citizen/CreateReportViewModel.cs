using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Citizen;

public class CreateReportViewModel
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }
}
