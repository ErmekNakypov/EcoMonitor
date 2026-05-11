using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Admin.Users;

public class CreateUserViewModel
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Password must be at most 100 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Send welcome email")]
    public bool SendWelcomeEmail { get; set; }

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}
