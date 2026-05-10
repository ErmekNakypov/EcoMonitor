using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainerById;

public class GetContainerByIdHandler : IRequestHandler<GetContainerByIdQuery, ContainerDetailsDto?>
{
    private readonly IApplicationDbContext _dbContext;

    public GetContainerByIdHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContainerDetailsDto?> Handle(GetContainerByIdQuery request, CancellationToken cancellationToken)
    {
        var container = await _dbContext.WasteContainers
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new ContainerDetailsDto(
                c.Id,
                c.Code,
                c.Address,
                c.Latitude,
                c.Longitude,
                c.Type,
                c.Status,
                c.Capacity,
                c.InstalledAt,
                c.CreatedAt,
                c.UpdatedAt,
                c.IsImported,
                c.OsmId))
            .FirstOrDefaultAsync(cancellationToken);

        return container;
    }
}
