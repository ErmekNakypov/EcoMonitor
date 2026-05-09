namespace EcoMonitor.Web.Models.Dashboard;

public class InspectorDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public int QueueSize { get; set; }
    public int MyActiveReports { get; set; }
    public int MyResolvedReports { get; set; }
}
