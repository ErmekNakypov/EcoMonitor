using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Sensors.GetContainerLatestFill;

public class GetContainerLatestFillHandler
    : IRequestHandler<GetContainerLatestFillQuery, ContainerLatestFillDto?>
{
    private readonly IApplicationDbContext _db;

    public GetContainerLatestFillHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ContainerLatestFillDto?> Handle(
        GetContainerLatestFillQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.WasteContainers
            .AsNoTracking()
            .Where(c => c.Id == request.ContainerId)
            .Select(c => new ContainerLatestFillDto(
                c.Id,
                c.Code,
                c.HeightCm,
                c.LastFillPercent,
                c.LastDistanceCm,
                c.LastMeasuredAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
