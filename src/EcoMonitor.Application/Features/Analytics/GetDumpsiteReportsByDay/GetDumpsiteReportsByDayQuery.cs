using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetDumpsiteReportsByDay;

public sealed record GetDumpsiteReportsByDayQuery() : IRequest<IReadOnlyList<DailyCount>>;

public sealed record DailyCount(DateOnly Date, int Count);
