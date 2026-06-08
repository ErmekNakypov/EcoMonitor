using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Account;

public class RegisterViewModel
{
    // [Display(Name)] and explicit ErrorMessage values are resource KEYS,
    // not literal labels. DataAnnotationLocalizerProvider looks them up
    // in Resources/Models/Account/RegisterViewModel.{culture}.resx. The
    // user-visible text lives in the .resx files.
    [Required]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "FullName")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "PasswordMaxLength")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "PasswordsDoNotMatch")]
    [Display(Name = "ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
