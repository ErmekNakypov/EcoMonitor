using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.CreateWasteContainer;

public class CreateWasteContainerHandler : IRequestHandler<CreateWasteContainerCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateWasteContainerHandler> _logger;

    public CreateWasteContainerHandler(IApplicationDbContext dbContext, ILogger<CreateWasteContainerHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateWasteContainerCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await _dbContext.WasteContainers
            .AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new DomainException("A container with this code already exists.");
        }

        var container = new WasteContainer
        {
            Code = request.Code,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Type = request.Type,
            Capacity = request.Capacity,
            InstalledAt = request.InstalledAt,
            Status = ContainerStatus.Active
        };

        _dbContext.WasteContainers.Add(container);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Container {Code} ({ContainerId}) created", container.Code, container.Id);
        return container.Id;
    }
}
