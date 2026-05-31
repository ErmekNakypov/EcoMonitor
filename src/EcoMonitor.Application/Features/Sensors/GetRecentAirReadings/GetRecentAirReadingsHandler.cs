using EcoMonitor.Application.Common.Interfaces;
using MediatR;

namespace EcoMonitor.Application.Features.Sensors.GetRecentAirReadings;

public class GetRecentAirReadingsHandler
    : IRequestHandler<GetRecentAirReadingsQuery, IReadOnlyList<AirReadingPointDto>>
{
    private readonly IAirQualityRepository _repository;

    public GetRecentAirReadingsHandler(IAirQualityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AirReadingPointDto>> Handle(
        GetRecentAirReadingsQuery request,
        CancellationToken cancellationToken)
    {
        var minutes = request.Minutes <= 0 ? 60 : Math.Min(request.Minutes, 24 * 60);
        var to = DateTime.UtcNow;
        var from = to.AddMinutes(-minutes);

        var readings = await _repository.GetReadingsAsync(request.StationId, from, to, cancellationToken);

        return readings
            .OrderBy(r => r.MeasuredAt)
            .Select(r => new AirReadingPointDto(
                r.MeasuredAt,
                r.Pm25,
                r.Pm10,
                r.Temperature,
                r.Humidity,
                r.AqiUs))
            .ToList();
    }
}
