using Microsoft.AspNetCore.Identity;

namespace EcoMonitor.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string PreferredLanguage { get; set; } = "ru";
    public bool IsActive { get; set; } = true;
}
