using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Containers;

public class ContainerListViewModel
{
    public string? SearchQuery { get; set; }
    public ContainerType? TypeFilter { get; set; }
    public ContainerStatus? StatusFilter { get; set; }
    public IReadOnlyList<ContainerListItemViewModel> Items { get; set; } = Array.Empty<ContainerListItemViewModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
