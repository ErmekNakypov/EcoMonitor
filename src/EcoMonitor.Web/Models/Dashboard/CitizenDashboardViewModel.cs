using EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;

namespace EcoMonitor.Web.Models.Dashboard;

public class CitizenDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<MyReportItemDto> RecentReports { get; set; } = Array.Empty<MyReportItemDto>();
    public int TotalReports { get; set; }
}
