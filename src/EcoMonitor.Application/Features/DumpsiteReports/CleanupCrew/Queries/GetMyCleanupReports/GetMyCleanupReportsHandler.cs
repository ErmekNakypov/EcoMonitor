using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetMyCleanupReports;

public class GetMyCleanupReportsHandler : IRequestHandler<GetMyCleanupReportsQuery, MyCleanupReportsResult>
{
    private static readonly DumpsiteStatus[] ActiveStatuses =
    {
        DumpsiteStatus.Confirmed,
        DumpsiteStatus.CleanupInProgress,
        DumpsiteStatus.AwaitingVerification
    };

    private static readonly DumpsiteStatus[] CompletedStatuses =
    {
        DumpsiteStatus.Resolved,
        DumpsiteStatus.Closed,
        DumpsiteStatus.Appealed
    };

    private readonly IApplicationDbContext _db;

    public GetMyCleanupReportsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MyCleanupReportsResult> Handle(GetMyCleanupReportsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var mine = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.CleanupCrewId == request.CleanupUserId);

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
            .Select(r => new MyCleanupReportDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.UpdatedAt,
                r.CleanupCompletedAt,
                r.ResolvedAt,
                r.ClosedAt))
            .ToListAsync(ct);

        return new MyCleanupReportsResult(rows, totalCount, page, pageSize, totalPages,
            activeCount, completedCount, overallCount);
    }
}
