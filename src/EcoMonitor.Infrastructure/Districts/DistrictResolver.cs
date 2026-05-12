using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Services;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcoMonitor.Infrastructure.Districts;

public sealed class DistrictResolver : IDistrictResolver
{
    private const string CacheKey = "districts_with_boundaries_v1";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public DistrictResolver(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<District?> ResolveAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var districts = await GetAllAsync(ct);

        foreach (var district in districts)
        {
            var polygon = district.Boundary
                .OrderBy(b => b.SequenceNumber)
                .Select(b => (b.Latitude, b.Longitude))
                .ToList();

            if (PointInPolygonChecker.IsPointInside(latitude, longitude, polygon))
            {
                return district;
            }
        }
        return null;
    }

    public async Task<IReadOnlyList<District>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<IReadOnlyList<District>>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var districts = await _db.Districts
            .AsNoTracking()
            .Include(d => d.Boundary)
            .ToListAsync(ct);

        _cache.Set(CacheKey, (IReadOnlyList<District>)districts, CacheTtl);
        return districts;
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);
}
