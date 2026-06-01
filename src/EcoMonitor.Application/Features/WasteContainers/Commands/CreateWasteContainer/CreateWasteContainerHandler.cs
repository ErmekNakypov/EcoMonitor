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
    private readonly IDistrictResolver _districtResolver;
    private readonly ILogger<CreateWasteContainerHandler> _logger;

    public CreateWasteContainerHandler(
        IApplicationDbContext dbContext,
        IDistrictResolver districtResolver,
        ILogger<CreateWasteContainerHandler> logger)
    {
        _dbContext = dbContext;
        _districtResolver = districtResolver;
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

        // Resolve once at create time so future "container is full" reports
        // can be routed straight to the district inspector. Containers outside
        // every polygon stay null and fall back to the broadcast notification
        // path (same convention as DumpsiteReport.DistrictId).
        var district = await _districtResolver.ResolveAsync(
            request.Latitude, request.Longitude, cancellationToken);

        var container = new WasteContainer
        {
            Code = request.Code,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Type = request.Type,
            Capacity = request.Capacity,
            InstalledAt = request.InstalledAt,
            Status = ContainerStatus.Active,
            DistrictId = district?.Id
        };

        _dbContext.WasteContainers.Add(container);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Container {Code} ({ContainerId}) created", container.Code, container.Id);
        return container.Id;
    }
}
