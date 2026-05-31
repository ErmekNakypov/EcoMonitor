using MediatR;

namespace EcoMonitor.Application.Features.Sensors.GetRecentFillReadings;

public sealed record GetRecentFillReadingsQuery(Guid ContainerId, int Minutes)
    : IRequest<IReadOnlyList<FillReadingPointDto>>;
