using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetAppealsQueue;

public class GetAppealsQueueHandler : IRequestHandler<GetAppealsQueueQuery, AppealsQueueResult>
{
    private readonly IApplicationDbContext _db;

    public GetAppealsQueueHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AppealsQueueResult> Handle(GetAppealsQueueQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.Appealed);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.Description.ToLower().Contains(s)
                || (r.AppealReason != null && r.AppealReason.ToLower().Contains(s)));
        }

        query = request.SortBy switch
        {
            "newest" => query.OrderByDescending(r => r.AppealedAt),
            _ => query.OrderBy(r => r.AppealedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AppealsQueueItemDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.AppealedAt ?? r.UpdatedAt,
                r.AppealReason ?? string.Empty))
            .ToListAsync(ct);

        return new AppealsQueueResult(rows, totalCount, page, pageSize, totalPages);
    }
}
