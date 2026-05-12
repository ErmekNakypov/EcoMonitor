using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetAutoTriageStats;

public sealed record GetAutoTriageStatsQuery() : IRequest<AutoTriageStatsDto>;

// AutoConfirmed = reports with AutoTriageReason IS NULL (passed every rule).
// Reviewed     = reports with AutoTriageReason IS NOT NULL (kicked to Inspector).
// Percentage   = AutoConfirmed / Total * 100, rounded.
public sealed record AutoTriageStatsDto(
    int Total,
    int AutoConfirmed,
    int Reviewed,
    double Percentage);
