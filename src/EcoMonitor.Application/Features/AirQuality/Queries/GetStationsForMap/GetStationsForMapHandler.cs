using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.AirQuality.Dto;
using MediatR;

namespace EcoMonitor.Application.Features.AirQuality.Queries.GetStationsForMap;

public class GetStationsForMapHandler : IRequestHandler<GetStationsForMapQuery, IReadOnlyList<StationWithLatestReadingDto>>
{
    private readonly IAirQualityRepository _repository;

    public GetStationsForMapHandler(IAirQualityRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<StationWithLatestReadingDto>> Handle(GetStationsForMapQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllStationsWithLatestReadingsAsync(cancellationToken);
    }
}
