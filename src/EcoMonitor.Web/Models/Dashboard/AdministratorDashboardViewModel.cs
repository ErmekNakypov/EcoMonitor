namespace EcoMonitor.Web.Models.Dashboard;

public class AdministratorDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int TotalReports { get; set; }
    public int TotalContainers { get; set; }
}
