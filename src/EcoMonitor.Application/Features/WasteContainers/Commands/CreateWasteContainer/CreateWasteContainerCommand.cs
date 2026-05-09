using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.CreateWasteContainer;

public sealed record CreateWasteContainerCommand(
    string Code,
    string Address,
    double Latitude,
    double Longitude,
    ContainerType Type,
    int Capacity,
    DateTime InstalledAt
) : IRequest<Guid>;
