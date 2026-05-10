using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.AirQuality.Dto;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class AirQualityRepository : IAirQualityRepository
{
    private const string LatestCacheKey = "stations:latest";
    private static readonly TimeSpan LatestCacheTtl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AirQualityRepository> _logger;

    public AirQualityRepository(ApplicationDbContext dbContext, IMemoryCache cache, ILogger<AirQualityRepository> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AirQualityStation> UpsertStationAsync(
        string externalId,
        string name,
        string? locality,
        string? providerName,
        double lat,
        double lng,
        AirQualitySource source,
        CancellationToken ct = default)
    {
        var existing = await _dbContext.AirQualityStations
            .FirstOrDefaultAsync(s => s.ExternalId == externalId && s.Source == source, ct);

        if (existing is null)
        {
            var station = new AirQualityStation
            {
                ExternalId = externalId,
                Name = name,
                Locality = locality,
                ProviderName = providerName,
                Latitude = lat,
                Longitude = lng,
                Source = source,
                IsActive = true
            };

            _dbContext.AirQualityStations.Add(station);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created air quality station {ExternalId} ({Name})", externalId, name);
            return station;
        }

        existing.Name = name;
        existing.Locality = locality;
        existing.ProviderName = providerName;
        existing.Latitude = lat;
        existing.Longitude = lng;

        await _dbContext.SaveChangesAsync(ct);
        return existing;
    }

    public async Task SaveReadingsAsync(IEnumerable<AirQualityReading> readings, CancellationToken ct = default)
    {
        var list = readings.ToList();
        if (list.Count == 0)
        {
            return;
        }

        _dbContext.AirQualityReadings.AddRange(list);

        var maxByStation = list
            .GroupBy(r => r.StationId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.MeasuredAt));

        var stationIds = maxByStation.Keys.ToList();
        var stations = await _dbContext.AirQualityStations
            .Where(s => stationIds.Contains(s.Id))
            .ToListAsync(ct);

        foreach (var station in stations)
        {
            var newest = maxByStation[station.Id];
            if (station.LastReadingAt is null || newest > station.LastReadingAt.Value)
            {
                station.LastReadingAt = newest;
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        _cache.Remove(LatestCacheKey);
    }

    public async Task<IReadOnlyList<StationWithLatestReadingDto>> GetAllStationsWithLatestReadingsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(LatestCacheKey, out IReadOnlyList<StationWithLatestReadingDto>? cached) && cached is not null)
        {
            return cached;
        }

        var rows = await (
            from s in _dbContext.AirQualityStations.AsNoTracking()
            where s.IsActive
            let latest = _dbContext.AirQualityReadings.AsNoTracking()
                .Where(r => r.StationId == s.Id)
                .OrderByDescending(r => r.MeasuredAt)
                .FirstOrDefault()
            select new StationWithLatestReadingDto(
                s.Id,
                s.Name,
                s.Locality,
                s.ProviderName,
                s.Latitude,
                s.Longitude,
                latest != null ? latest.Pm25 : null,
                latest != null ? latest.Pm10 : null,
                latest != null ? latest.Temperature : null,
                latest != null ? latest.Humidity : null,
                latest != null ? latest.Pressure : null,
                latest != null ? (DateTime?)latest.MeasuredAt : null,
                s.Source)
        ).ToListAsync(ct);

        _cache.Set(LatestCacheKey, (IReadOnlyList<StationWithLatestReadingDto>)rows, LatestCacheTtl);
        return rows;
    }

    public async Task<StationDetailsDto?> GetStationByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await (
            from s in _dbContext.AirQualityStations.AsNoTracking()
            where s.Id == id
            let latest = _dbContext.AirQualityReadings.AsNoTracking()
                .Where(r => r.StationId == s.Id)
                .OrderByDescending(r => r.MeasuredAt)
                .FirstOrDefault()
            select new StationDetailsDto(
                s.Id,
                s.ExternalId,
                s.Name,
                s.Locality,
                s.ProviderName,
                s.Latitude,
                s.Longitude,
                s.Source,
                latest != null ? latest.Pm25 : null,
                latest != null ? latest.Pm10 : null,
                latest != null ? latest.Temperature : null,
                latest != null ? latest.Humidity : null,
                latest != null ? latest.Pressure : null,
                latest != null ? (DateTime?)latest.MeasuredAt : null)
        ).FirstOrDefaultAsync(ct);

        return row;
    }

    public async Task<IReadOnlyList<AirQualityReading>> GetReadingsAsync(Guid stationId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _dbContext.AirQualityReadings
            .AsNoTracking()
            .Where(r => r.StationId == stationId && r.MeasuredAt >= fromUtc && r.MeasuredAt <= toUtc)
            .OrderBy(r => r.MeasuredAt)
            .ToListAsync(ct);
    }
}
