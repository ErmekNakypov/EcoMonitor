using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoMonitor.Web.Models.CleanupCrew;

public class StartCleanupInputModel
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one before-cleanup photo is required.")]
    public List<IFormFile> BeforePhotos { get; set; } = new();
}

public class CompleteCleanupInputModel
{
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one after-cleanup photo is required.")]
    public List<IFormFile> AfterPhotos { get; set; } = new();
}
