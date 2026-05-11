using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetVerificationQueue;

public sealed record GetVerificationQueueQuery(int Page = 1, int PageSize = 20) : IRequest<VerificationQueueResult>;

public sealed record VerificationQueueResult(
    IReadOnlyList<VerificationItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record VerificationItemDto(
    Guid Id,
    string ShortDescription,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    string? CleanupCrewName,
    DateTime? CleanupCompletedAt);
