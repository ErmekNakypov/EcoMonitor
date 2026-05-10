using EcoMonitor.Application.Features.AirQuality.Dto;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IAirQualityRepository
{
    Task<AirQualityStation> UpsertStationAsync(
        string externalId,
        string name,
        string? locality,
        string? providerName,
        double lat,
        double lng,
        AirQualitySource source,
        CancellationToken ct = default);

    /// <summary>
    /// Inserts only readings whose (StationId, MeasuredAt) pair does not already exist.
    /// Returns the number of newly inserted rows. LastReadingAt is bumped from the full
    /// batch (duplicates still confirm freshness).
    /// </summary>
    Task<int> SaveReadingsAsync(IEnumerable<AirQualityReading> readings, CancellationToken ct = default);

    Task<IReadOnlyList<StationWithLatestReadingDto>> GetAllStationsWithLatestReadingsAsync(CancellationToken ct = default);

    Task<StationDetailsDto?> GetStationByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AirQualityReading>> GetReadingsAsync(Guid stationId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
