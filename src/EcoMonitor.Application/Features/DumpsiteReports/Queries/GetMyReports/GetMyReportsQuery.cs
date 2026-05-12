using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;

public sealed record GetMyReportsQuery(
    Guid ReporterId,
    string Tab = "all",
    int Page = 1,
    int PageSize = 10) : IRequest<MyReportsResult>;

public sealed record MyReportsResult(
    IReadOnlyList<MyReportItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int ActiveCount,
    int ResolvedCount,
    int RejectedCount,
    int OverallCount);

public sealed record MyReportItemDto(
    Guid Id,
    string Description,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> PhotoPaths,
    DateTime CreatedAt);
