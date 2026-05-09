using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Inspector;

public class RejectInputModel
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;
}
