using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetVerificationQueue;

public class GetVerificationQueueHandler : IRequestHandler<GetVerificationQueueQuery, VerificationQueueResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IUserLookupService _userLookup;

    public GetVerificationQueueHandler(IApplicationDbContext db, IUserLookupService userLookup)
    {
        _db = db;
        _userLookup = userLookup;
    }

    public async Task<VerificationQueueResult> Handle(GetVerificationQueueQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.AwaitingVerification);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .OrderBy(r => r.CleanupCompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Description,
                r.Latitude,
                r.Longitude,
                FirstPhotoPath = r.PhotoPaths.FirstOrDefault(),
                r.CleanupCrewId,
                r.CleanupCompletedAt
            })
            .ToListAsync(ct);

        var crewIds = rows.Where(r => r.CleanupCrewId.HasValue).Select(r => r.CleanupCrewId!.Value).Distinct().ToList();
        IReadOnlyDictionary<Guid, UserSummaryDto> users = crewIds.Count > 0
            ? await _userLookup.GetByIdsAsync(crewIds, ct)
            : new Dictionary<Guid, UserSummaryDto>();

        var items = rows.Select(r => new VerificationItemDto(
            r.Id,
            r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
            r.Latitude,
            r.Longitude,
            r.FirstPhotoPath,
            r.CleanupCrewId.HasValue ? users.GetValueOrDefault(r.CleanupCrewId.Value)?.FullName : null,
            r.CleanupCompletedAt)).ToList();

        return new VerificationQueueResult(items, totalCount, page, pageSize, totalPages);
    }
}
