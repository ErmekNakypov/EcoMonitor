using EcoMonitor.Application.Features.DumpsiteReports.Common;
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
    IReadOnlyList<string> InspectionPhotos,
    IReadOnlyList<string> BeforeCleanupPhotos,
    IReadOnlyList<string> AfterCleanupPhotos,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? AppealedAt,
    string? AppealReason,
    IReadOnlyList<string> AppealPhotos,
    DateTime? AppealReviewedAt,
    string? AppealResolutionNotes,
    AppealOutcome? AppealOutcome,
    DateTime? ClosedAt,
    string? CleanupCrewName,
    DateTime? CleanupCompletedAt,
    string? VerifiedByInspectorName,
    IReadOnlyList<ReportEventDto> Events);
