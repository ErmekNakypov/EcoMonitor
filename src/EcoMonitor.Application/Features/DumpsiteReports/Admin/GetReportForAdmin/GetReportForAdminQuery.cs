using EcoMonitor.Application.Features.DumpsiteReports.Common;
using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Admin.GetReportForAdmin;

public sealed record GetReportForAdminQuery(Guid ReportId) : IRequest<AdminReportDto?>;

public sealed record AdminReportDto(
    Guid Id,
    string Description,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> PhotoPaths,
    Guid? ReporterId,
    string? ReporterEmail,
    string? ReporterFullName,
    DateTime? ReporterRegisteredAt,
    Guid? AssignedInspectorId,
    string? AssignedInspectorEmail,
    string? AssignedInspectorFullName,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ReportSource Source,
    string? TelegramUserName,
    IReadOnlyList<ReportEventDto> Events,
    DateTime? CleanupFlaggedAt,
    string? DistrictNameRu,
    string? DistrictColorHex);
