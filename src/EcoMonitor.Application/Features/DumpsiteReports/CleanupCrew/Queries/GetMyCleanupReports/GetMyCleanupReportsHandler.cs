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

    private readonly IApplicationDbContext _db;

    public GetMyCleanupReportsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MyCleanupReportsResult> Handle(GetMyCleanupReportsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.CleanupCrewId == request.CleanupUserId
                        && ActiveStatuses.Contains(r.Status));

        var totalCount = await query.CountAsync(ct);
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
                r.UpdatedAt))
            .ToListAsync(ct);

        return new MyCleanupReportsResult(rows, totalCount, page, pageSize, totalPages);
    }
}
