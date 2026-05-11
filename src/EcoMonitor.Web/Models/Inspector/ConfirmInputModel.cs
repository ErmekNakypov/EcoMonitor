using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoMonitor.Web.Models.Inspector;

public class ConfirmInputModel
{
    [StringLength(1000, MinimumLength = 0)]
    public string? Observations { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one inspection photo is required.")]
    public List<IFormFile> Photos { get; set; } = new();
}

public class RejectCleanupInputModel
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}
