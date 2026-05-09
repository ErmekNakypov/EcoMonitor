using EcoMonitor.Domain.Entities;

namespace EcoMonitor.Web.Models.Dashboard;

public class CitizenDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<DumpsiteReport> RecentReports { get; set; } = Array.Empty<DumpsiteReport>();
}
