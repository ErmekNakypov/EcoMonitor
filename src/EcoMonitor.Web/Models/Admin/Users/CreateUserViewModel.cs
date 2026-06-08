using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Admin.Users;

public class CreateUserViewModel
{
    // [Display(Name)] and explicit ErrorMessage values are resource KEYS
    // looked up via DataAnnotationLocalizerProvider in
    // Resources/Models/Admin/Users/CreateUserViewModel.{culture}.resx.
    // The user-visible text lives in the .resx files.
    [Required]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "FullName")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "PasswordMaxLength")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "SendWelcomeEmail")]
    public bool SendWelcomeEmail { get; set; }

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}
