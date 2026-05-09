namespace EcoMonitor.Web.Models.Shared;

public sealed class BreadcrumbsViewModel
{
    public List<BreadcrumbItem> Items { get; init; } = new();
}

public sealed class BreadcrumbItem
{
    public string Label { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? Icon { get; init; }
}
