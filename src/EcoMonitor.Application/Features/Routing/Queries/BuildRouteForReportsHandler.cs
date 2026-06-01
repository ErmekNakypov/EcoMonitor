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

        // Optional geolocation origin: if the queue page captured the user's
        // current position, prepend it to the points list as a virtual node
        // at index 0. The unchanged NearestNeighborOrder seeds from index 0,
        // so the algorithm naturally walks origin → nearest report → next →
        // … and the resulting TotalDistance includes the origin→first leg.
        RouteOrigin? origin = null;
        var hasOrigin = request.StartLat is { } startLat && request.StartLng is { } startLng;
        var points = new List<(double Lat, double Lng)>(ordered.Count + (hasOrigin ? 1 : 0));
        if (hasOrigin)
        {
            origin = new RouteOrigin(request.StartLat!.Value, request.StartLng!.Value);
            points.Add((origin.Latitude, origin.Longitude));
        }
        points.AddRange(ordered.Select(r => (r!.Latitude, r.Longitude)));

        var visitOrder = RouteCalculator.NearestNeighborOrder(points);

        var orderedPoints = visitOrder.Select(i => points[i]).ToList();
        var totalKm = RouteCalculator.TotalDistance(orderedPoints);
        var minutes = RouteCalculator.EstimatedMinutes(totalKm);

        // visitOrder is over the augmented points list; when an origin is
        // present it sits at index 0 of that list. Skip it when materialising
        // RouteStops (the origin is reported separately via RouteResult.Origin),
        // and map the remaining indices back to the underlying report rows.
        var stops = new List<RouteStop>(ordered.Count);
        var orderNumber = 0;
        for (var i = 0; i < visitOrder.Count; i++)
        {
            var pointIndex = visitOrder[i];
            if (hasOrigin && pointIndex == 0)
            {
                continue;
            }
            var reportIndex = hasOrigin ? pointIndex - 1 : pointIndex;
            var r = ordered[reportIndex]!;
            var title = r.Description.Length > 80
                ? r.Description.Substring(0, 80) + "…"
                : r.Description;
            orderNumber++;
            stops.Add(new RouteStop(
                OrderNumber: orderNumber,
                ReportId: r.Id,
                Title: title,
                DistrictName: r.DistrictName,
                DistrictColorHex: r.DistrictColorHex,
                Status: r.Status,
                Latitude: r.Latitude,
                Longitude: r.Longitude));
        }

        return new RouteResult(stops, totalKm, minutes, origin);
    }
}
