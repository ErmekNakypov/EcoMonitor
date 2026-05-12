using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetCleanupQueue;

public class GetCleanupQueueHandler : IRequestHandler<GetCleanupQueueQuery, CleanupQueueResult>
{
    private readonly IApplicationDbContext _db;

    public GetCleanupQueueHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CleanupQueueResult> Handle(GetCleanupQueueQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.Confirmed && r.CleanupCrewId == null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r => r.Description.ToLower().Contains(s));
        }

        if (string.Equals(request.Source, "web", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Source == ReportSource.Web);
        }
        else if (string.Equals(request.Source, "telegram", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Source == ReportSource.Telegram);
        }

        query = request.SortBy switch
        {
            "newest" => query.OrderByDescending(r => r.UpdatedAt),
            _ => query.OrderBy(r => r.UpdatedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CleanupQueueItemDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.UpdatedAt,
                r.Source))
            .ToListAsync(ct);

        return new CleanupQueueResult(rows, totalCount, page, pageSize, totalPages);
    }
}
