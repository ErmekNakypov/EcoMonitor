using MediatR;

namespace EcoMonitor.Application.Features.Sensors.GetContainerLatestFill;

public sealed record GetContainerLatestFillQuery(Guid ContainerId)
    : IRequest<ContainerLatestFillDto?>;
