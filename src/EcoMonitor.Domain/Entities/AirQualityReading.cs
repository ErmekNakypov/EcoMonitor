using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class AirQualityReading : BaseEntity
{
    public Guid StationId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Pm25 { get; set; }
    public double? Pm10 { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? Pressure { get; set; }
    public DateTime MeasuredAt { get; set; }
    public AirQualitySource Source { get; set; }
}
