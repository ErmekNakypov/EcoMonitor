using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Web.Models.Account;

public class LoginViewModel
{
    // [Display(Name)] values double as the resource KEYS that
    // DataAnnotationLocalizerProvider looks up in
    // Resources/Models/Account/LoginViewModel.{culture}.resx — so the
    // values intentionally match the property name (no spaces) rather
    // than reading like a UI label. The translated label is what the
    // user sees; the Name= here is purely the lookup key.
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "RememberMe")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
