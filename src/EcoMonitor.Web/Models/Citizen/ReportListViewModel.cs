namespace EcoMonitor.Web.Models.Citizen;

public class ReportListViewModel
{
    public string Tab { get; set; } = "all";
    public IReadOnlyList<ReportListItemViewModel> Reports { get; set; } = Array.Empty<ReportListItemViewModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int ActiveCount { get; set; }
    public int ResolvedCount { get; set; }
    public int RejectedCount { get; set; }
    public int OverallCount { get; set; }
}
