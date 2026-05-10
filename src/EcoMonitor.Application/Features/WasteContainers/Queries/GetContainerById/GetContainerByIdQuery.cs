using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainerById;

public sealed record GetContainerByIdQuery(Guid Id) : IRequest<ContainerDetailsDto?>;

public sealed record ContainerDetailsDto(
    Guid Id,
    string Code,
    string Address,
    double Latitude,
    double Longitude,
    ContainerType Type,
    ContainerStatus Status,
    int Capacity,
    DateTime InstalledAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsImported,
    long? OsmId);
