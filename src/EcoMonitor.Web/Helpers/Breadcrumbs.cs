using EcoMonitor.Web.Models.Shared;

namespace EcoMonitor.Web.Helpers;

public static class Breadcrumbs
{
    public static BreadcrumbsViewModel Build(params BreadcrumbItem[] items) =>
        new() { Items = items.ToList() };
}
