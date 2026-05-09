using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Citizen;

public class ReportListItemViewModel
{
    public Guid Id { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public DumpsiteStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FirstPhotoPath { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
}
