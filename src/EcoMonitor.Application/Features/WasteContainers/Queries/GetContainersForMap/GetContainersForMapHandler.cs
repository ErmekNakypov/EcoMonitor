using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersForMap;

public class GetContainersForMapHandler : IRequestHandler<GetContainersForMapQuery, IReadOnlyList<ContainerMapPointDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetContainersForMapHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ContainerMapPointDto>> Handle(GetContainersForMapQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.WasteContainers
            .AsNoTracking()
            .Where(c => c.Status != ContainerStatus.Removed)
            .Select(c => new ContainerMapPointDto(
                c.Id,
                c.Code,
                c.Address,
                c.Latitude,
                c.Longitude,
                c.Type,
                c.Status,
                c.Capacity,
                c.InstalledAt))
            .ToListAsync(cancellationToken);
    }
}
