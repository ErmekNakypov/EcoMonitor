using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetDumpsiteStatusBreakdown;

public class GetDumpsiteStatusBreakdownHandler : IRequestHandler<GetDumpsiteStatusBreakdownQuery, IReadOnlyList<StatusCount>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetDumpsiteStatusBreakdownHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StatusCount>> Handle(GetDumpsiteStatusBreakdownQuery request, CancellationToken cancellationToken)
    {
        var counts = await _dbContext.DumpsiteReports
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(r => r.Status, r => r.Count);

        // Preserve enum declaration order so legend reads chronologically through the workflow
        var ordered = new[]
        {
            DumpsiteStatus.New,
            DumpsiteStatus.InReview,
            DumpsiteStatus.Confirmed,
            DumpsiteStatus.Resolved,
            DumpsiteStatus.Rejected
        };

        return ordered
            .Where(s => byStatus.ContainsKey(s))
            .Select(s => new StatusCount(s.GetDisplayName(), byStatus[s], ColorFor(s)))
            .ToList();
    }

    private static string ColorFor(DumpsiteStatus status) => status switch
    {
        DumpsiteStatus.New => "#9CA3AF",
        DumpsiteStatus.InReview => "#3B82F6",
        DumpsiteStatus.Confirmed => "#EF4444",
        DumpsiteStatus.Rejected => "#71717A",
        DumpsiteStatus.Resolved => "#10B981",
        _ => "#9CA3AF"
    };
}
