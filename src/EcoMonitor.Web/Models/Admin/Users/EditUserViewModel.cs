using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Admin.Users;

public class EditUserViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public string CurrentRole { get; set; } = string.Empty;

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}
