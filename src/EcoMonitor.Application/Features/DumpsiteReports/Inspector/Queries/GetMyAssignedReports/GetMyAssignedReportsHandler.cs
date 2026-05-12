using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;

public class GetMyAssignedReportsHandler : IRequestHandler<GetMyAssignedReportsQuery, MyAssignedResult>
{
    private static readonly DumpsiteStatus[] ActiveStatuses =
    {
        DumpsiteStatus.InReview,
        DumpsiteStatus.AwaitingVerification,
        DumpsiteStatus.Appealed,
        DumpsiteStatus.FlaggedByCleanupCrew
    };

    private static readonly DumpsiteStatus[] CompletedStatuses =
    {
        DumpsiteStatus.Confirmed,
        DumpsiteStatus.CleanupInProgress,
        DumpsiteStatus.Resolved,
        DumpsiteStatus.Closed,
        DumpsiteStatus.Rejected
    };

    private readonly IApplicationDbContext _dbContext;

    public GetMyAssignedReportsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyAssignedResult> Handle(GetMyAssignedReportsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        var inspectorId = request.InspectorId;

        // Inspector touches a report through three roles: original assignee,
        // verifier of the cleanup, and appeal reviewer. Any of the three counts.
        var mine = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.AssignedInspectorId == inspectorId
                     || r.VerifiedByInspectorId == inspectorId
                     || r.AppealReviewedByInspectorId == inspectorId);

        var counts = await mine
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeCount = counts.Where(c => ActiveStatuses.Contains(c.Status)).Sum(c => c.Count);
        var completedCount = counts.Where(c => CompletedStatuses.Contains(c.Status)).Sum(c => c.Count);
        var overallCount = counts.Sum(c => c.Count);

        var tab = (request.Tab ?? "active").ToLowerInvariant();
        var query = tab switch
        {
            "completed" => mine.Where(r => CompletedStatuses.Contains(r.Status)),
            "all" => mine,
            _ => mine.Where(r => ActiveStatuses.Contains(r.Status))
        };

        var totalCount = tab switch
        {
            "completed" => completedCount,
            "all" => overallCount,
            _ => activeCount
        };
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MyAssignedItemDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.CreatedAt,
                r.UpdatedAt,
                r.ResolvedAt,
                r.ClosedAt,
                r.AssignedInspectorId == inspectorId,
                r.VerifiedByInspectorId == inspectorId,
                r.AppealReviewedByInspectorId == inspectorId))
            .ToListAsync(ct);

        return new MyAssignedResult(rows, totalCount, page, pageSize, totalPages,
            activeCount, completedCount, overallCount);
    }
}
