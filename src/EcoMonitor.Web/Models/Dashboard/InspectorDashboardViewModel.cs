namespace EcoMonitor.Web.Models.Dashboard;

public class InspectorDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public int AssignedReports { get; set; }
    public int NewUnassignedReports { get; set; }
}
