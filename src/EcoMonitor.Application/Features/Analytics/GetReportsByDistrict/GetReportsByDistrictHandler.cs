using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetReportsByDistrict;

public class GetReportsByDistrictHandler : IRequestHandler<GetReportsByDistrictQuery, ReportsByDistrictResult>
{
    private const int WindowDays = 30;

    private readonly IApplicationDbContext _db;

    public GetReportsByDistrictHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ReportsByDistrictResult> Handle(GetReportsByDistrictQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-WindowDays);
        var districts = await _db.Districts
            .AsNoTracking()
            .OrderBy(d => d.NameRu)
            .ToListAsync(ct);

        // Single roundtrip: group reports by (districtId, status) bucket.
        var grouped = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.CreatedAt >= since)
            .GroupBy(r => new { r.DistrictId, r.Status })
            .Select(g => new { g.Key.DistrictId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var stats = new List<DistrictStat>();
        foreach (var district in districts)
        {
            var rows = grouped.Where(g => g.DistrictId == district.Id).ToList();
            stats.Add(BuildStat(district.Code, district.NameRu, district.ColorHex, rows.Select(r => (r.Status, r.Count))));
        }
        var outsideRows = grouped.Where(g => g.DistrictId == null).ToList();
        if (outsideRows.Count > 0)
        {
            stats.Add(BuildStat("OUTSIDE", "Outside city", "#9CA3AF",
                outsideRows.Select(r => (r.Status, r.Count))));
        }
        return new ReportsByDistrictResult(stats);
    }

    private static DistrictStat BuildStat(string code, string nameRu, string colorHex,
        IEnumerable<(DumpsiteStatus Status, int Count)> rows)
    {
        var rowList = rows.ToList();
        int total = rowList.Sum(r => r.Count);
        int resolved = rowList.Where(r => r.Status == DumpsiteStatus.Resolved || r.Status == DumpsiteStatus.Closed)
                               .Sum(r => r.Count);
        int rejected = rowList.Where(r => r.Status == DumpsiteStatus.Rejected).Sum(r => r.Count);
        int inProgress = rowList.Where(r =>
            r.Status == DumpsiteStatus.InReview
            || r.Status == DumpsiteStatus.Confirmed
            || r.Status == DumpsiteStatus.CleanupInProgress
            || r.Status == DumpsiteStatus.AwaitingVerification
            || r.Status == DumpsiteStatus.Appealed
            || r.Status == DumpsiteStatus.FlaggedByCleanupCrew).Sum(r => r.Count);
        return new DistrictStat(code, nameRu, colorHex, total, resolved, rejected, inProgress);
    }
}
