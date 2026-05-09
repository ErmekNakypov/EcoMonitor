using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetReportDetails;

public sealed record GetReportDetailsQuery(Guid ReportId, Guid RequestingUserId) : IRequest<ReportDetailsDto?>;

public sealed record ReportDetailsDto(
    Guid Id,
    string Description,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> PhotoPaths,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
