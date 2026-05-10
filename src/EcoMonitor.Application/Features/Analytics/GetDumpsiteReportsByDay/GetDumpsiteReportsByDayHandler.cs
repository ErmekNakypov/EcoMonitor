using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetDumpsiteReportsByDay;

public class GetDumpsiteReportsByDayHandler : IRequestHandler<GetDumpsiteReportsByDayQuery, IReadOnlyList<DailyCount>>
{
    private const int DayWindow = 30;

    private readonly IApplicationDbContext _dbContext;

    public GetDumpsiteReportsByDayHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DailyCount>> Handle(GetDumpsiteReportsByDayQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var startUtc = today.AddDays(-(DayWindow - 1));

        var rows = await _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.CreatedAt >= startUtc)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byDate = rows.ToDictionary(r => DateOnly.FromDateTime(r.Date), r => r.Count);

        var result = new List<DailyCount>(DayWindow);
        for (int i = 0; i < DayWindow; i++)
        {
            var d = DateOnly.FromDateTime(startUtc.AddDays(i));
            result.Add(new DailyCount(d, byDate.GetValueOrDefault(d, 0)));
        }
        return result;
    }
}
