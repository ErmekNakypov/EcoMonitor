using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportForInspector;

public sealed record GetReportForInspectorQuery(Guid ReportId) : IRequest<InspectorReportDto?>;

public sealed record InspectorReportDto(
    Guid Id,
    string Description,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> PhotoPaths,
    Guid? ReporterId,
    string? ReporterEmail,
    string? ReporterFullName,
    Guid? AssignedInspectorId,
    string? AssignedInspectorEmail,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ReportSource Source,
    string? TelegramUserName,
    string? InspectorObservations,
    IReadOnlyList<string> InspectionPhotos,
    IReadOnlyList<string> CleanupBeforePhotos,
    IReadOnlyList<string> CleanupAfterPhotos,
    Guid? CleanupCrewId,
    string? CleanupCrewName,
    DateTime? CleanupStartedAt,
    DateTime? CleanupCompletedAt,
    string? CleanupNotes,
    string? AutoTriageReason,
    long? TelegramUserId,
    int ReporterTotalReports,
    int ReporterPendingReports,
    int ReporterResolvedReports,
    int ReporterRejectedReports);
