namespace EcoMonitor.Web.Models.Dashboard;

public class AdministratorDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int TotalReports { get; set; }
    public int TotalContainers { get; set; }

    // Auto-triage rate over the last 30 days. Total covers all reports in
    // the window; AutoConfirmed is how many landed at Confirmed without
    // inspector intervention. Percentage = AutoConfirmed / Total * 100.
    public int AutoTriageTotal30d { get; set; }
    public int AutoTriageAutoConfirmed30d { get; set; }
    public int AutoTriageReviewed30d { get; set; }
    public double AutoTriagePercentage30d { get; set; }

    public int AppealsTotalEligible30d { get; set; }
    public int AppealsAppealed30d { get; set; }
    public double AppealsPercentage30d { get; set; }
}
