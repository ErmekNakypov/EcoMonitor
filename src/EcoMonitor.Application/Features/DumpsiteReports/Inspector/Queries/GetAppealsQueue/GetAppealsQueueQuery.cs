using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetAppealsQueue;

public sealed record GetAppealsQueueQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null) : IRequest<AppealsQueueResult>;

public sealed record AppealsQueueResult(
    IReadOnlyList<AppealsQueueItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record AppealsQueueItemDto(
    Guid Id,
    string ShortDescription,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime AppealedAt,
    string AppealReason);
