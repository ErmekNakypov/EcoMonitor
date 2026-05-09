using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportQueue;

namespace EcoMonitor.Web.Models.Inspector;

public class QueueListViewModel
{
    public IReadOnlyList<QueueItemDto> Items { get; set; } = Array.Empty<QueueItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
