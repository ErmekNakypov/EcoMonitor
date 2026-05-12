using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetFlagStats;

public sealed record GetFlagStatsQuery() : IRequest<FlagStatsDto>;

// Total = reports that left the Confirmed gate in the last 30 days
// (anything assigned to a cleanup crew = had a chance to be flagged).
// Flagged = those that were ever flagged on site (CleanupFlaggedAt != null),
// regardless of current status — Dismissed-back and Reassigned still count.
public sealed record FlagStatsDto(
    int TotalEligible,
    int Flagged,
    double Percentage);
