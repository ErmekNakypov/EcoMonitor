using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Admin.Users;

public class ResetPasswordViewModel
{
    public Guid UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Password must be at most 100 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
