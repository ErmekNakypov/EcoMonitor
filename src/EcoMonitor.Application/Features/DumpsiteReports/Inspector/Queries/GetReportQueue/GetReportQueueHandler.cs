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

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new QueueItemDto(
                r.Id,
                r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
                r.Latitude,
                r.Longitude,
                r.PhotoPaths.FirstOrDefault(),
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReportQueueResult(rows, totalCount, page, pageSize, totalPages);
    }
}
