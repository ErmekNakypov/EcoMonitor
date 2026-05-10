using EcoMonitor.Web.Helpers;

namespace EcoMonitor.Web.Models.Air;

public class AirQualitySummaryViewModel
{
    public double? AverageAqiUs { get; set; }
    public AqiLevel AqiLevel { get; set; }
    public int ActiveStations { get; set; }
    public int TotalStations { get; set; }
}
