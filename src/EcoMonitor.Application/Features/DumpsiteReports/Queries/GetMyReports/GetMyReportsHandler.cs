using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;

public class GetMyReportsHandler : IRequestHandler<GetMyReportsQuery, MyReportsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetMyReportsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyReportsResult> Handle(GetMyReportsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var query = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.ReporterId == request.ReporterId);

        var totalCount = await query.CountAsync(cancellationToken);
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

        return new MyReportsResult(rows, totalCount, page, pageSize, totalPages);
    }
}
