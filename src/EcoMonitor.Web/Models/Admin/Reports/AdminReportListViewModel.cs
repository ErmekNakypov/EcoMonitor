using EcoMonitor.Application.Features.DumpsiteReports.Admin.GetAllReports;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Reports;

public class AdminReportListViewModel
{
    public DumpsiteStatus? StatusFilter { get; set; }
    public IReadOnlyList<AllReportsItemDto> Items { get; set; } = Array.Empty<AllReportsItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
