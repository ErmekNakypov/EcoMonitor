using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Inspector;

public class ResolveInputModel
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    [Display(Name = "Resolution notes")]
    public string Notes { get; set; } = string.Empty;
}
