using EcoMonitor.Application.Features.AirQuality.Dto;
using MediatR;

namespace EcoMonitor.Application.Features.AirQuality.Queries.GetStationsForMap;

public sealed record GetStationsForMapQuery() : IRequest<IReadOnlyList<StationWithLatestReadingDto>>;
