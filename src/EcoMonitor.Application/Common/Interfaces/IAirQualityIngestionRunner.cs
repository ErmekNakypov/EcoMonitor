namespace EcoMonitor.Application.Common.Interfaces;

public interface IAirQualityIngestionRunner
{
    Task<IngestionResult> RunOnceAsync(CancellationToken ct = default);
}

public sealed record IngestionResult(
    int TotalReadingsSaved,
    int DuplicatesSkipped,
    IReadOnlyList<ProviderRunResult> ProviderResults,
    string? Error);

public sealed record ProviderRunResult(string ProviderName, int Saved, string? Error);
