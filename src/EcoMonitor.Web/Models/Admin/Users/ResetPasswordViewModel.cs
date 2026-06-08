using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Admin.Users;

public class ResetPasswordViewModel
{
    public Guid UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    // [Display(Name)] and ErrorMessage values are resource KEYS looked up
    // via DataAnnotationLocalizerProvider in
    // Resources/Models/Admin/Users/ResetPasswordViewModel.{culture}.resx.
    [Required]
    [StringLength(100, ErrorMessage = "PasswordMaxLength")]
    [DataType(DataType.Password)]
    [Display(Name = "NewPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "PasswordsDoNotMatch")]
    [Display(Name = "ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
