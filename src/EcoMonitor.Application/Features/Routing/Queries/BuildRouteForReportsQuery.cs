using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.Routing.Queries;

public sealed record BuildRouteForReportsQuery(IReadOnlyList<Guid> ReportIds)
    : IRequest<RouteResult>;

public sealed record RouteResult(
    IReadOnlyList<RouteStop> Stops,
    double TotalDistanceKm,
    int EstimatedMinutes);

public sealed record RouteStop(
    int OrderNumber,
    Guid ReportId,
    string Title,
    string? DistrictName,
    string? DistrictColorHex,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude);
