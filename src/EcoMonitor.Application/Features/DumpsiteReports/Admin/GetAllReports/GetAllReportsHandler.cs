using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Admin.GetAllReports;

public class GetAllReportsHandler : IRequestHandler<GetAllReportsQuery, AllReportsResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserLookupService _userLookup;

    public GetAllReportsHandler(IApplicationDbContext dbContext, IUserLookupService userLookup)
    {
        _dbContext = dbContext;
        _userLookup = userLookup;
    }

    public async Task<AllReportsResult> Handle(GetAllReportsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _dbContext.DumpsiteReports.AsNoTracking().AsQueryable();

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
            .Select(r => new
            {
                r.Id,
                r.Description,
                r.Status,
                r.ReporterId,
                r.AssignedInspectorId,
                FirstPhotoPath = r.PhotoPaths.FirstOrDefault(),
                PhotoCount = r.PhotoPaths.Count,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.ReporterId)
            .Concat(rows.Where(r => r.AssignedInspectorId.HasValue).Select(r => r.AssignedInspectorId!.Value))
            .Distinct()
            .ToList();

        var users = await _userLookup.GetByIdsAsync(userIds, cancellationToken);

        var items = rows.Select(r =>
        {
            var reporter = users.GetValueOrDefault(r.ReporterId);
            var inspector = r.AssignedInspectorId.HasValue
                ? users.GetValueOrDefault(r.AssignedInspectorId.Value)
                : null;

            return new AllReportsItemDto(
                r.Id,
                r.Description.Length > 100 ? r.Description.Substring(0, 100) + "…" : r.Description,
                r.Status,
                r.ReporterId,
                reporter?.Email ?? "(unknown)",
                reporter?.FullName ?? "(unknown)",
                r.AssignedInspectorId,
                inspector?.Email,
                r.FirstPhotoPath,
                r.PhotoCount,
                r.CreatedAt,
                r.UpdatedAt);
        }).ToList();

        return new AllReportsResult(items, totalCount, page, pageSize, totalPages);
    }
}
