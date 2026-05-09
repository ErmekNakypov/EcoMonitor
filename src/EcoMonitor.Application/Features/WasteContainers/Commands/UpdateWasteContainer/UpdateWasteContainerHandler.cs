using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.UpdateWasteContainer;

public class UpdateWasteContainerHandler : IRequestHandler<UpdateWasteContainerCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UpdateWasteContainerHandler> _logger;

    public UpdateWasteContainerHandler(IApplicationDbContext dbContext, ILogger<UpdateWasteContainerHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateWasteContainerCommand request, CancellationToken cancellationToken)
    {
        var container = await _dbContext.WasteContainers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (container is null)
        {
            throw new NotFoundException($"Container {request.Id} not found.");
        }

        container.Code = request.Code;
        container.Address = request.Address;
        container.Latitude = request.Latitude;
        container.Longitude = request.Longitude;
        container.Type = request.Type;
        container.Capacity = request.Capacity;
        container.Status = request.Status;
        container.InstalledAt = request.InstalledAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Container {Code} ({ContainerId}) updated", container.Code, container.Id);
        return Unit.Value;
    }
}
