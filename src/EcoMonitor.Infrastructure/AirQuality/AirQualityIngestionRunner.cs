using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class AirQualityIngestionRunner : IAirQualityIngestionRunner
{
    private readonly IAirQualityProvider _provider;
    private readonly IAirQualityRepository _repository;
    private readonly ILogger<AirQualityIngestionRunner> _logger;

    public AirQualityIngestionRunner(
        IAirQualityProvider provider,
        IAirQualityRepository repository,
        ILogger<AirQualityIngestionRunner> logger)
    {
        _provider = provider;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IngestionResult> RunOnceAsync(CancellationToken ct = default)
    {
        try
        {
            var readings = await _provider.FetchCurrentReadingsAsync(ct);
            if (readings.Count == 0)
            {
                return new IngestionResult(_provider.Name, 0, 0, "Provider returned no data");
            }

            var dbReadings = new List<AirQualityReading>(readings.Count);
            foreach (var r in readings)
            {
                var station = await _repository.UpsertStationAsync(
                    r.ExternalId, r.StationName, r.Locality, r.ProviderName,
                    r.Latitude, r.Longitude, r.Source, ct);

                dbReadings.Add(new AirQualityReading
                {
                    StationId = station.Id,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Pm25 = r.Pm25,
                    Pm10 = r.Pm10,
                    Temperature = r.Temperature,
                    Humidity = r.Humidity,
                    Pressure = r.Pressure,
                    MeasuredAt = r.MeasuredAt,
                    Source = r.Source
                });
            }

            await _repository.SaveReadingsAsync(dbReadings, ct);

            _logger.LogInformation("Ingestion completed: {Count} readings from {Provider}", dbReadings.Count, _provider.Name);
            return new IngestionResult(_provider.Name, readings.Count, dbReadings.Count, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed");
            return new IngestionResult(_provider.Name, 0, 0, ex.Message);
        }
    }
}
