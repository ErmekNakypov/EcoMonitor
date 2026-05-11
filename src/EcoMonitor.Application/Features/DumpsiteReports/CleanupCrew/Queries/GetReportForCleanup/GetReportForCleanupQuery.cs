using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetReportForCleanup;

public sealed record GetReportForCleanupQuery(Guid ReportId) : IRequest<CleanupReportDto?>;

public sealed record CleanupReportDto(
    Guid Id,
    string Description,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> CitizenPhotos,
    IReadOnlyList<string> InspectionPhotos,
    IReadOnlyList<string> CleanupBeforePhotos,
    IReadOnlyList<string> CleanupAfterPhotos,
    string? InspectorObservations,
    string? CleanupNotes,
    Guid? CleanupCrewId,
    DateTime? CleanupStartedAt,
    DateTime? CleanupCompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
