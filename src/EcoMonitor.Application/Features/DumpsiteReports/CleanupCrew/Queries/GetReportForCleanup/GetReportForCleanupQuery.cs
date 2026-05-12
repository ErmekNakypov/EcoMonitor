using EcoMonitor.Application.Features.DumpsiteReports.Common;
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
    IReadOnlyList<string> AppealPhotos,
    string? InspectorObservations,
    string? CleanupNotes,
    Guid? CleanupCrewId,
    DateTime? CleanupStartedAt,
    DateTime? CleanupCompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ReportSource Source,
    string? ReporterFullName,
    string? ReporterEmail,
    string? TelegramUserName,
    long? TelegramUserId,
    int ReporterTotalReports,
    int ReporterPendingReports,
    int ReporterResolvedReports,
    int ReporterRejectedReports,
    string? AutoTriageReason,
    string? ConfirmingInspectorName,
    DateTime? ConfirmedAt,
    int ReworkCount,
    DateTime? ReworkStartedAt,
    DateTime? AppealedAt,
    string? AppealReason,
    DateTime? AppealReviewedAt,
    string? AppealResolutionNotes,
    AppealOutcome? AppealOutcome,
    IReadOnlyList<ReportEventDto> Events,
    string? CleanupRejectionReason,
    string? CleanupRejectionNotes,
    DateTime? CleanupFlaggedAt,
    Guid? CleanupFlaggedByCrewId,
    int ReassignCount,
    IReadOnlyList<string> FlagEvidencePhotos,
    string? DistrictNameRu,
    string? DistrictColorHex);
