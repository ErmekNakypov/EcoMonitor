using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetFlaggedReports;

public sealed record GetFlaggedReportsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null) : IRequest<FlaggedReportsResult>;

public sealed record FlaggedReportsResult(
    IReadOnlyList<FlaggedReportItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record FlaggedReportItemDto(
    Guid Id,
    string ShortDescription,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime FlaggedAt,
    string FlagReasonCode,
    string FlagReasonDisplay,
    string? FlagNotes,
    string? CrewName,
    int ReassignCount);
