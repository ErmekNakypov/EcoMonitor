using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportQueue;

public class GetReportQueueHandler : IRequestHandler<GetReportQueueQuery, ReportQueueResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetReportQueueHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportQueueResult> Handle(GetReportQueueQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.New && r.AssignedInspectorId == null);

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

        // "My district only" — keep just the reports whose district is owned
        // by the current inspector. Reports outside any district drop out.
        if (request.OnlyDistrictInspectorId is { } inspectorId)
        {
            var myDistrictIds = await _dbContext.Districts
                .AsNoTracking()
                .Where(d => d.AssignedInspectorId == inspectorId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(r => r.DistrictId != null && myDistrictIds.Contains(r.DistrictId.Value));
        }

        query = request.SortBy switch
        {
            "newest" => query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderBy(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new QueueItemDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.CreatedAt,
                r.Source,
                r.District != null ? r.District.NameRu : null,
                r.District != null ? r.District.ColorHex : null))
            .ToListAsync(cancellationToken);

        return new ReportQueueResult(rows, totalCount, page, pageSize, totalPages);
    }
}
