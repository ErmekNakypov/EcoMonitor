using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;

public class GetMyAssignedReportsHandler : IRequestHandler<GetMyAssignedReportsQuery, MyAssignedResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetMyAssignedReportsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyAssignedResult> Handle(GetMyAssignedReportsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.AssignedInspectorId == request.InspectorId);

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(r => r.Status == request.StatusFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
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
                r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new MyAssignedResult(rows, totalCount, page, pageSize, totalPages);
    }
}
