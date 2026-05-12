using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;

public sealed record GetMyAssignedReportsQuery(
    Guid InspectorId,
    string Tab = "active",
    int Page = 1,
    int PageSize = 20) : IRequest<MyAssignedResult>;

public sealed record MyAssignedResult(
    IReadOnlyList<MyAssignedItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int ActiveCount,
    int CompletedCount,
    int OverallCount);

public sealed record MyAssignedItemDto(
    Guid Id,
    string ShortDescription,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    string? FirstPhotoPath,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    bool WasAssigned,
    bool WasVerifier,
    bool WasAppealReviewer);
