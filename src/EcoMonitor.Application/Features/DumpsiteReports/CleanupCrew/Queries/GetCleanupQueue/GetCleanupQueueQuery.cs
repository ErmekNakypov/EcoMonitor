using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetCleanupQueue;

public sealed record GetCleanupQueueQuery(int Page = 1, int PageSize = 20) : IRequest<CleanupQueueResult>;

public sealed record CleanupQueueResult(
    IReadOnlyList<CleanupQueueItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record CleanupQueueItemDto(
    Guid Id,
    string ShortDescription,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime ConfirmedAt);
