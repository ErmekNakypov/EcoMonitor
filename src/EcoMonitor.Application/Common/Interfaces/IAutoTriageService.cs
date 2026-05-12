using EcoMonitor.Domain.Entities;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IAutoTriageService
{
    Task<TriageDecision> EvaluateAsync(DumpsiteReport report, CancellationToken ct = default);
}

public sealed record TriageDecision(
    bool ShouldAutoConfirm,
    string? RejectionReason);
