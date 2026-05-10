using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class AirQualityIngestionRunner : IAirQualityIngestionRunner
{
    private readonly IEnumerable<IAirQualityProvider> _providers;
    private readonly IAirQualityRepository _repository;
    private readonly ILogger<AirQualityIngestionRunner> _logger;

    public AirQualityIngestionRunner(
        IEnumerable<IAirQualityProvider> providers,
        IAirQualityRepository repository,
        ILogger<AirQualityIngestionRunner> logger)
    {
        _providers = providers;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IngestionResult> RunOnceAsync(CancellationToken ct = default)
    {
        var providerResults = new List<ProviderRunResult>();
        var allReadings = new List<AirQualityReading>();

        foreach (var provider in _providers)
        {
            try
            {
                var readings = await provider.FetchCurrentReadingsAsync(ct);
                if (readings.Count == 0)
                {
                    providerResults.Add(new ProviderRunResult(provider.Name, 0, null));
                    continue;
                }

                foreach (var r in readings)
                {
                    var station = await _repository.UpsertStationAsync(
                        r.ExternalId, r.StationName, r.Locality, r.ProviderName,
                        r.Latitude, r.Longitude, r.Source, ct);

                    allReadings.Add(new AirQualityReading
                    {
                        StationId = station.Id,
                        Latitude = r.Latitude,
                        Longitude = r.Longitude,
                        Pm25 = r.Pm25,
                        Pm10 = r.Pm10,
                        Temperature = r.Temperature,
                        Humidity = r.Humidity,
                        Pressure = r.Pressure,
                        AqiUs = r.AqiUs,
                        MeasuredAt = r.MeasuredAt,
                        Source = r.Source
                    });
                }

                providerResults.Add(new ProviderRunResult(provider.Name, readings.Count, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Provider {Provider} failed during ingestion", provider.Name);
                providerResults.Add(new ProviderRunResult(provider.Name, 0, ex.Message));
            }
        }

        if (allReadings.Count == 0)
        {
            _logger.LogInformation("Ingestion completed: no readings from any provider");
            return new IngestionResult(0, 0, providerResults, null);
        }

        var inserted = await _repository.SaveReadingsAsync(allReadings, ct);
        var skipped = allReadings.Count - inserted;

        var breakdown = string.Join(" + ",
            providerResults.Where(p => p.Saved > 0).Select(p => $"{p.Saved} {p.ProviderName}"));

        _logger.LogInformation(
            "Ingestion completed: {Inserted} new readings ({Breakdown}), {Skipped} duplicates skipped",
            inserted,
            string.IsNullOrEmpty(breakdown) ? "no providers contributed" : breakdown,
            skipped);

        return new IngestionResult(inserted, skipped, providerResults, null);
    }
}
