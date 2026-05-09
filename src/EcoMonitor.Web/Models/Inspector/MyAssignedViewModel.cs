using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Inspector;

public class MyAssignedViewModel
{
    public DumpsiteStatus? StatusFilter { get; set; }
    public IReadOnlyList<MyAssignedItemDto> Items { get; set; } = Array.Empty<MyAssignedItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
