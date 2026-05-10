using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetAverageAqiByDay;

public sealed record GetAverageAqiByDayQuery() : IRequest<IReadOnlyList<DailyAvg>>;

public sealed record DailyAvg(DateOnly Date, double? AvgAqiUs, int StationCount);
