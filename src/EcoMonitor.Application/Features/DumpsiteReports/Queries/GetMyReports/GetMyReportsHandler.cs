using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;

public class GetMyReportsHandler : IRequestHandler<GetMyReportsQuery, MyReportsResult>
{
    private static readonly DumpsiteStatus[] ActiveStatuses =
    {
        DumpsiteStatus.New,
        DumpsiteStatus.InReview,
        DumpsiteStatus.Confirmed,
        DumpsiteStatus.CleanupInProgress,
        DumpsiteStatus.AwaitingVerification,
        DumpsiteStatus.Appealed
    };

    private static readonly DumpsiteStatus[] ResolvedStatuses =
    {
        DumpsiteStatus.Resolved,
        DumpsiteStatus.Closed
    };

    private readonly IApplicationDbContext _dbContext;

    public GetMyReportsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyReportsResult> Handle(GetMyReportsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var mine = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.ReporterId == request.ReporterId);

        var counts = await mine
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var activeCount = counts.Where(c => ActiveStatuses.Contains(c.Status)).Sum(c => c.Count);
        var resolvedCount = counts.Where(c => ResolvedStatuses.Contains(c.Status)).Sum(c => c.Count);
        var rejectedCount = counts.Where(c => c.Status == DumpsiteStatus.Rejected).Sum(c => c.Count);
        var overallCount = counts.Sum(c => c.Count);

        var tab = (request.Tab ?? "all").ToLowerInvariant();
        var query = tab switch
        {
            "active" => mine.Where(r => ActiveStatuses.Contains(r.Status)),
            "resolved" => mine.Where(r => ResolvedStatuses.Contains(r.Status)),
            "rejected" => mine.Where(r => r.Status == DumpsiteStatus.Rejected),
            _ => mine
        };

        var totalCount = tab switch
        {
            "active" => activeCount,
            "resolved" => resolvedCount,
            "rejected" => rejectedCount,
            _ => overallCount
        };
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MyReportItemDto(
                r.Id,
                r.Description,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new MyReportsResult(rows, totalCount, page, pageSize, totalPages,
            activeCount, resolvedCount, rejectedCount, overallCount);
    }
}
