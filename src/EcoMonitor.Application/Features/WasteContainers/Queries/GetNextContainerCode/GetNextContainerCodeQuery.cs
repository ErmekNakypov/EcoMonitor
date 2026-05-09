using MediatR;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetNextContainerCode;

public sealed record GetNextContainerCodeQuery() : IRequest<string>;
