using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetMyCleanupReports;

public sealed record GetMyCleanupReportsQuery(
    Guid CleanupUserId,
    string Tab = "active",
    int Page = 1,
    int PageSize = 20) : IRequest<MyCleanupReportsResult>;

public sealed record MyCleanupReportsResult(
    IReadOnlyList<MyCleanupReportDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int ActiveCount,
    int CompletedCount,
    int OverallCount);

public sealed record MyCleanupReportDto(
    Guid Id,
    string ShortDescription,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime UpdatedAt,
    DateTime? CleanupCompletedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt);
