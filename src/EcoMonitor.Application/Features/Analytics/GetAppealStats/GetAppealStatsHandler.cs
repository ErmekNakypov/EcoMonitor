using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetAppealStats;

public class GetAppealStatsHandler : IRequestHandler<GetAppealStatsQuery, AppealStatsDto>
{
    private const int WindowDays = 30;

    private readonly IApplicationDbContext _db;

    public GetAppealStatsHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AppealStatsDto> Handle(GetAppealStatsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-WindowDays);
        var eligibleStatuses = new[]
        {
            DumpsiteStatus.Resolved,
            DumpsiteStatus.Appealed,
            DumpsiteStatus.Closed
        };

        var pool = _db.DumpsiteReports.AsNoTracking()
            .Where(r => r.ResolvedAt != null
                        && r.ResolvedAt >= since
                        && eligibleStatuses.Contains(r.Status));

        var totalEligible = await pool.CountAsync(ct);
        if (totalEligible == 0)
        {
            return new AppealStatsDto(0, 0, 0);
        }

        var appealed = await pool.CountAsync(r => r.AppealedAt != null, ct);
        var percentage = Math.Round(appealed * 100.0 / totalEligible, 1);
        return new AppealStatsDto(totalEligible, appealed, percentage);
    }
}
