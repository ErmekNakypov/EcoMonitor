using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.DeleteWasteContainer;

public class DeleteWasteContainerHandler : IRequestHandler<DeleteWasteContainerCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeleteWasteContainerHandler> _logger;

    public DeleteWasteContainerHandler(IApplicationDbContext dbContext, ILogger<DeleteWasteContainerHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteWasteContainerCommand request, CancellationToken cancellationToken)
    {
        var container = await _dbContext.WasteContainers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (container is null)
        {
            throw new NotFoundException($"Container {request.Id} not found.");
        }

        container.Status = ContainerStatus.Removed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Container {Code} ({ContainerId}) marked as removed", container.Code, container.Id);
        return Unit.Value;
    }
}
