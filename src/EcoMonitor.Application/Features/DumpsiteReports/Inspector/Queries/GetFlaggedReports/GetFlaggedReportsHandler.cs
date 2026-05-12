using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.FlagCleanup;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetFlaggedReports;

public class GetFlaggedReportsHandler : IRequestHandler<GetFlaggedReportsQuery, FlaggedReportsResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IUserLookupService _userLookup;

    public GetFlaggedReportsHandler(IApplicationDbContext db, IUserLookupService userLookup)
    {
        _db = db;
        _userLookup = userLookup;
    }

    public async Task<FlaggedReportsResult> Handle(GetFlaggedReportsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.FlaggedByCleanupCrew);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r => r.Description.ToLower().Contains(s));
        }

        query = request.SortBy switch
        {
            "newest" => query.OrderByDescending(r => r.CleanupFlaggedAt),
            _ => query.OrderBy(r => r.CleanupFlaggedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Description,
                r.Latitude,
                r.Longitude,
                FirstPhotoPath = r.PhotoPaths.FirstOrDefault(),
                r.CleanupFlaggedAt,
                r.CleanupRejectionReason,
                r.CleanupRejectionNotes,
                r.CleanupFlaggedByCrewId,
                r.ReassignCount
            })
            .ToListAsync(ct);

        var crewIds = rows
            .Where(r => r.CleanupFlaggedByCrewId.HasValue)
            .Select(r => r.CleanupFlaggedByCrewId!.Value)
            .Distinct()
            .ToList();
        IReadOnlyDictionary<Guid, UserSummaryDto> users = crewIds.Count > 0
            ? await _userLookup.GetByIdsAsync(crewIds, ct)
            : new Dictionary<Guid, UserSummaryDto>();

        var items = rows.Select(r => new FlaggedReportItemDto(
            r.Id,
            r.Description.Length > 160 ? r.Description.Substring(0, 160) + "…" : r.Description,
            r.Latitude,
            r.Longitude,
            r.FirstPhotoPath,
            r.CleanupFlaggedAt ?? DateTime.UtcNow,
            r.CleanupRejectionReason ?? string.Empty,
            FlagCleanupReasons.Display(r.CleanupRejectionReason),
            r.CleanupRejectionNotes,
            r.CleanupFlaggedByCrewId.HasValue
                ? users.GetValueOrDefault(r.CleanupFlaggedByCrewId.Value)?.FullName
                : null,
            r.ReassignCount)).ToList();

        return new FlaggedReportsResult(items, totalCount, page, pageSize, totalPages);
    }
}
