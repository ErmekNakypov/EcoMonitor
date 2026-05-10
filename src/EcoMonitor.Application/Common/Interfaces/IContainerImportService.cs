namespace EcoMonitor.Application.Common.Interfaces;

public interface IContainerImportService
{
    Task<ContainerImportResult> ImportFromOsmAsync(CancellationToken ct = default);
}

public sealed record ContainerImportResult(
    int TotalFetched,
    int Created,
    int Updated,
    int Skipped,
    string? Error,
    string Source = "unknown");
