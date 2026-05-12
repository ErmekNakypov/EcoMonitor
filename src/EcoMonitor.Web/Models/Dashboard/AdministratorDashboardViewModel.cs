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

    public int FlagsTotalEligible30d { get; set; }
    public int FlagsFlagged30d { get; set; }
    public double FlagsPercentage30d { get; set; }

    public List<DistrictReportSlice> DistrictBreakdown30d { get; set; } = new();
}

public class DistrictReportSlice
{
    public string Code { get; set; } = string.Empty;
    public string NameRu { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Resolved { get; set; }
    public int Rejected { get; set; }
    public int InProgress { get; set; }
}
