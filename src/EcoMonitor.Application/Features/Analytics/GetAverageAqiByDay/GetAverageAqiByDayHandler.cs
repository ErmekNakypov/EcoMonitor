using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.Analytics.GetAverageAqiByDay;

public class GetAverageAqiByDayHandler : IRequestHandler<GetAverageAqiByDayQuery, IReadOnlyList<DailyAvg>>
{
    private const int DayWindow = 7;

    private readonly IApplicationDbContext _dbContext;

    public GetAverageAqiByDayHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DailyAvg>> Handle(GetAverageAqiByDayQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var startUtc = today.AddDays(-(DayWindow - 1));

        var rows = await _dbContext.AirQualityReadings
            .AsNoTracking()
            .Where(r => r.MeasuredAt >= startUtc)
            .GroupBy(r => r.MeasuredAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                AvgAqi = g.Where(r => r.AqiUs != null).Average(r => (double?)r.AqiUs),
                StationCount = g.Select(r => r.StationId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        var byDate = rows.ToDictionary(
            r => DateOnly.FromDateTime(r.Date),
            r => (Avg: r.AvgAqi, Stations: r.StationCount));

        var result = new List<DailyAvg>(DayWindow);
        for (int i = 0; i < DayWindow; i++)
        {
            var d = DateOnly.FromDateTime(startUtc.AddDays(i));
            if (byDate.TryGetValue(d, out var v))
            {
                result.Add(new DailyAvg(d, v.Avg, v.Stations));
            }
            else
            {
                result.Add(new DailyAvg(d, null, 0));
            }
        }
        return result;
    }
}
