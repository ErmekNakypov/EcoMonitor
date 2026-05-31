using MediatR;

namespace EcoMonitor.Application.Features.Sensors.GetRecentAirReadings;

public sealed record GetRecentAirReadingsQuery(Guid StationId, int Minutes)
    : IRequest<IReadOnlyList<AirReadingPointDto>>;
