using EcoMonitor.Application.Features.AirQuality.Dto;
using EcoMonitor.Domain.Entities;
using MediatR;

namespace EcoMonitor.Application.Features.AirQuality.Queries.GetStationDetails;

public sealed record GetStationDetailsQuery(Guid Id) : IRequest<StationFullDetailsDto?>;

public sealed record StationFullDetailsDto(StationDetailsDto Station, IReadOnlyList<AirQualityReading> Last24Hours);
