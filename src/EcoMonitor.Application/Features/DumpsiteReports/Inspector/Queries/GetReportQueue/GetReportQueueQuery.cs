using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportQueue;

public sealed record GetReportQueueQuery(int Page = 1, int PageSize = 20) : IRequest<ReportQueueResult>;

public sealed record ReportQueueResult(
    IReadOnlyList<QueueItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record QueueItemDto(
    Guid Id,
    string ShortDescription,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime CreatedAt);
