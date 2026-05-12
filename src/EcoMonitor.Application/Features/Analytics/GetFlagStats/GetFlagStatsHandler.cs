using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetFlagStats;

public class GetFlagStatsHandler : IRequestHandler<GetFlagStatsQuery, FlagStatsDto>
{
    private const int WindowDays = 30;

    private readonly IApplicationDbContext _db;

    public GetFlagStatsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FlagStatsDto> Handle(GetFlagStatsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-WindowDays);

        // Eligibility: any report whose Confirmed gate was crossed in the
        // window — proxy is "report has been picked up by a cleanup crew".
        // CleanupCrewId is set during TakeForCleanup; we use UpdatedAt as a
        // coarse window filter to avoid scanning the entire table.
        var pool = _db.DumpsiteReports.AsNoTracking()
            .Where(r => r.UpdatedAt >= since && r.CleanupCrewId != null);

        var totalEligible = await pool.CountAsync(ct);
        if (totalEligible == 0)
        {
            return new FlagStatsDto(0, 0, 0);
        }

        var flagged = await pool.CountAsync(r => r.CleanupFlaggedAt != null, ct);
        var percentage = Math.Round(flagged * 100.0 / totalEligible, 1);
        return new FlagStatsDto(totalEligible, flagged, percentage);
    }
}
