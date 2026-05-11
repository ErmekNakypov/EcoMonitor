namespace EcoMonitor.Application.Common.Models;

public sealed class AppOptions
{
    public const string Section = "App";

    // Absolute base URL used to compose links inside outbound emails (e.g.,
    // "Open report" buttons) because notification services run outside of an
    // HTTP request context. Override per environment via the "App:BaseUrl"
    // configuration key.
    public string BaseUrl { get; set; } = "http://localhost:5108";
}
