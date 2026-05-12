using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Services.Routing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Routing.Queries;

public class BuildRouteForReportsHandler
    : IRequestHandler<BuildRouteForReportsQuery, RouteResult>
{
    private readonly IApplicationDbContext _db;

    public BuildRouteForReportsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RouteResult> Handle(BuildRouteForReportsQuery request, CancellationToken ct)
    {
        if (request.ReportIds.Count == 0)
        {
            return new RouteResult(Array.Empty<RouteStop>(), 0, 0);
        }

        var raw = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => request.ReportIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Description,
                r.Status,
                r.Latitude,
                r.Longitude,
                DistrictName = r.District != null ? r.District.NameRu : null,
                DistrictColorHex = r.District != null ? r.District.ColorHex : null
            })
            .ToListAsync(ct);

        if (raw.Count == 0)
        {
            return new RouteResult(Array.Empty<RouteStop>(), 0, 0);
        }

        // Preserve caller-supplied order for deterministic "start point"
        // — nearest-neighbor begins from index 0 of the input list.
        var ordered = request.ReportIds
            .Select(id => raw.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .ToList();

        var points = ordered.Select(r => (r!.Latitude, r.Longitude)).ToList();
        var visitOrder = RouteCalculator.NearestNeighborOrder(points);

        var orderedPoints = visitOrder.Select(i => points[i]).ToList();
        var totalKm = RouteCalculator.TotalDistance(orderedPoints);
        var minutes = RouteCalculator.EstimatedMinutes(totalKm);

        var stops = new List<RouteStop>(visitOrder.Count);
        for (var i = 0; i < visitOrder.Count; i++)
        {
            var r = ordered[visitOrder[i]]!;
            var title = r.Description.Length > 80
                ? r.Description.Substring(0, 80) + "…"
                : r.Description;
            stops.Add(new RouteStop(
                OrderNumber: i + 1,
                ReportId: r.Id,
                Title: title,
                DistrictName: r.DistrictName,
                DistrictColorHex: r.DistrictColorHex,
                Status: r.Status,
                Latitude: r.Latitude,
                Longitude: r.Longitude));
        }

        return new RouteResult(stops, totalKm, minutes);
    }
}
