using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetDumpsiteStatusBreakdown;

public sealed record GetDumpsiteStatusBreakdownQuery() : IRequest<IReadOnlyList<StatusCount>>;

public sealed record StatusCount(string StatusName, int Count, string ColorHex);
