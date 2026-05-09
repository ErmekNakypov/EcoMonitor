using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersList;

public sealed record GetContainersListQuery(
    string? Search,
    ContainerType? TypeFilter,
    ContainerStatus? StatusFilter,
    int Page = 1,
    int PageSize = 20
) : IRequest<ContainersListResult>;

public sealed record ContainersListResult(
    IReadOnlyList<ContainerListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record ContainerListItemDto(
    Guid Id,
    string Code,
    string Address,
    double Latitude,
    double Longitude,
    ContainerType Type,
    ContainerStatus Status,
    int Capacity,
    DateTime InstalledAt,
    DateTime CreatedAt);
