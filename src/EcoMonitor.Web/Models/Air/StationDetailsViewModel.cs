using EcoMonitor.Application.Features.AirQuality.Dto;
using EcoMonitor.Application.Features.AirQuality.Queries.GetStationDetails;
using EcoMonitor.Domain.Entities;

namespace EcoMonitor.Web.Models.Air;

public class StationDetailsViewModel
{
    public StationDetailsDto Station { get; }
    public IReadOnlyList<AirQualityReading> Last24Hours { get; }

    public StationDetailsViewModel(StationFullDetailsDto details)
    {
        Station = details.Station;
        Last24Hours = details.Last24Hours;
    }
}
