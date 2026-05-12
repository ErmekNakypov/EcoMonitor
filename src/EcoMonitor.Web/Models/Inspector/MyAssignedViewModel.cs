using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;

namespace EcoMonitor.Web.Models.Inspector;

public class MyAssignedViewModel
{
    public string Tab { get; set; } = "active";
    public IReadOnlyList<MyAssignedItemDto> Items { get; set; } = Array.Empty<MyAssignedItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverallCount { get; set; }
}
