using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Admin.GetAllReports;

public sealed record GetAllReportsQuery(DumpsiteStatus? StatusFilter, int Page = 1, int PageSize = 20)
    : IRequest<AllReportsResult>;

public sealed record AllReportsResult(
    IReadOnlyList<AllReportsItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record AllReportsItemDto(
    Guid Id,
    string ShortDescription,
    DumpsiteStatus Status,
    Guid ReporterId,
    string ReporterEmail,
    string ReporterFullName,
    Guid? AssignedInspectorId,
    string? AssignedInspectorEmail,
    string? FirstPhotoPath,
    int PhotoCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
