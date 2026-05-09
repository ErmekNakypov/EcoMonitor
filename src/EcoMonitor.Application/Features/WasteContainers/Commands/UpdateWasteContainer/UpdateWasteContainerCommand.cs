using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.UpdateWasteContainer;

public sealed record UpdateWasteContainerCommand(
    Guid Id,
    string Code,
    string Address,
    double Latitude,
    double Longitude,
    ContainerType Type,
    int Capacity,
    ContainerStatus Status,
    DateTime InstalledAt
) : IRequest<Unit>;
