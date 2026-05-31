using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Sensors.GetRecentFillReadings;

public class GetRecentFillReadingsHandler
    : IRequestHandler<GetRecentFillReadingsQuery, IReadOnlyList<FillReadingPointDto>>
{
    private readonly IApplicationDbContext _db;

    public GetRecentFillReadingsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FillReadingPointDto>> Handle(
        GetRecentFillReadingsQuery request,
        CancellationToken cancellationToken)
    {
        var minutes = request.Minutes <= 0 ? 60 : Math.Min(request.Minutes, 24 * 60);
        var from = DateTime.UtcNow.AddMinutes(-minutes);

        var rows = await _db.ContainerFillReadings
            .AsNoTracking()
            .Where(r => r.ContainerId == request.ContainerId && r.MeasuredAt >= from)
            .OrderBy(r => r.MeasuredAt)
            .Select(r => new FillReadingPointDto(
                r.MeasuredAt,
                r.DistanceCm,
                r.FillPercent,
                r.BatteryMv))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
