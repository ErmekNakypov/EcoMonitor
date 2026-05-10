namespace EcoMonitor.Application.Common.Interfaces;

public interface IAirQualityIngestionRunner
{
    Task<IngestionResult> RunOnceAsync(CancellationToken ct = default);
}

public sealed record IngestionResult(
    string Provider,
    int StationsTouched,
    int ReadingsSaved,
    int DuplicatesSkipped,
    string? Error);
