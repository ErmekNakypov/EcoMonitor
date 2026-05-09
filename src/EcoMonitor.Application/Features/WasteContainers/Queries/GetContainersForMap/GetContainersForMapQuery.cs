using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersForMap;

public sealed record GetContainersForMapQuery() : IRequest<IReadOnlyList<ContainerMapPointDto>>;

public sealed record ContainerMapPointDto(
    Guid Id,
    string Code,
    string Address,
    double Latitude,
    double Longitude,
    ContainerType Type,
    ContainerStatus Status,
    int Capacity,
    DateTime InstalledAt);
