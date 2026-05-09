namespace EcoMonitor.Web.Models.Shared;

public sealed class EmptyStateViewModel
{
    public string Icon { get; init; } = "bi-inbox";
    public string Title { get; init; } = "Nothing here yet";
    public string? Subtitle { get; init; }
    public string? CtaText { get; init; }
    public string? CtaUrl { get; init; }
    public string? CtaIcon { get; init; }
}
