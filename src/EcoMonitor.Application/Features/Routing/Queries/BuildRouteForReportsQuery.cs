using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.Routing.Queries;

// StartLat / StartLng are the (optional) crew or inspector geolocation captured
// on the queue page right before the form posts. When both are present the
// handler prepends them as a virtual origin node so nearest-neighbor visits
// the closest report first. When either is null (denial, timeout, no browser
// support, http on a non-localhost host) the handler falls back to today's
// behaviour: seed from the first selected report.
public sealed record BuildRouteForReportsQuery(
    IReadOnlyList<Guid> ReportIds,
    double? StartLat = null,
    double? StartLng = null)
    : IRequest<RouteResult>;

public sealed record RouteResult(
    IReadOnlyList<RouteStop> Stops,
    double TotalDistanceKm,
    int EstimatedMinutes,
    RouteOrigin? Origin = null);

public sealed record RouteStop(
    int OrderNumber,
    Guid ReportId,
    string Title,
    string? DistrictName,
    string? DistrictColorHex,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude);

// Virtual start node: only coordinates. Has no report id, no district, no
// status, no link — the view renders it as a "Start: your current location"
// marker and prepends it to the polyline. The distance from this point to
// the first numbered stop IS included in TotalDistanceKm.
public sealed record RouteOrigin(double Latitude, double Longitude);
