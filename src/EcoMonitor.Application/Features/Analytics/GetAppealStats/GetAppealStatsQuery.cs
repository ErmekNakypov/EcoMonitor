using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetAppealStats;

public sealed record GetAppealStatsQuery() : IRequest<AppealStatsDto>;

// Percentage = Appealed / (Resolved + Appealed + Closed_with_appeal) over the
// last 30 days. We count any report whose AppealedAt is non-null as "appealed"
// regardless of current status, because Dismissed/Upheld appeals end up
// outside Status==Appealed.
public sealed record AppealStatsDto(
    int TotalEligible,
    int Appealed,
    double Percentage);
