using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.DeleteWasteContainer;

public sealed record DeleteWasteContainerCommand(Guid Id) : IRequest<Unit>;
