using EcoMonitor.Application.Common.Interfaces;
using MediatR;

namespace EcoMonitor.Application.Features.AirQuality.Queries.GetStationDetails;

public class GetStationDetailsHandler : IRequestHandler<GetStationDetailsQuery, StationFullDetailsDto?>
{
    private readonly IAirQualityRepository _repository;

    public GetStationDetailsHandler(IAirQualityRepository repository)
    {
        _repository = repository;
    }

    public async Task<StationFullDetailsDto?> Handle(GetStationDetailsQuery request, CancellationToken cancellationToken)
    {
        var station = await _repository.GetStationByIdAsync(request.Id, cancellationToken);
        if (station is null)
        {
            return null;
        }

        var to = DateTime.UtcNow;
        var from = to.AddHours(-24);
        var readings = await _repository.GetReadingsAsync(request.Id, from, to, cancellationToken);

        return new StationFullDetailsDto(station, readings);
    }
}
