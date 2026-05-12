using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetAutoTriageStats;

public class GetAutoTriageStatsHandler : IRequestHandler<GetAutoTriageStatsQuery, AutoTriageStatsDto>
{
    private const int WindowDays = 30;

    private readonly IApplicationDbContext _dbContext;

    public GetAutoTriageStatsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AutoTriageStatsDto> Handle(GetAutoTriageStatsQuery request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-WindowDays);

        var windowQuery = _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.CreatedAt >= since);

        var total = await windowQuery.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new AutoTriageStatsDto(0, 0, 0, 0);
        }

        var autoConfirmed = await windowQuery
            .CountAsync(r => r.AutoTriageReason == null, cancellationToken);
        var reviewed = total - autoConfirmed;
        var percentage = Math.Round(autoConfirmed * 100.0 / total, 1);

        return new AutoTriageStatsDto(total, autoConfirmed, reviewed, percentage);
    }
}
