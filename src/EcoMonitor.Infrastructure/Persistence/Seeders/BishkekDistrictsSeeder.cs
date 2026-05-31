using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Infrastructure.Districts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Persistence.Seeders;

// Seeds the 4 administrative districts of Bishkek as irregular polygons
// that visually approximate real city districts. Vertices are not exact
// administrative borders (those need an official geodatabase) — they
// share edges at common points so the four polygons tile without gaps.
//
// SeedAsync is no-op when districts already exist (first-run seed).
// ReseedBoundariesAsync rewrites the boundary points of the four known
// districts in place, preserving district IDs and inspector assignments —
// safe to run repeatedly via the admin diagnostics button.
public static class BishkekDistrictsSeeder
{
    private sealed record DistrictSpec(
        string Code,
        string NameRu,
        string NameEn,
        string NameKy,
        string ColorHex,
        (double Lat, double Lng)[] Vertices);

    // Vertices in clockwise order. Shared border vertices use identical
    // coordinates across neighbouring polygons so adjacent districts tile
    // without visual gaps.
    private static readonly DistrictSpec[] Specs =
    {
        new("OKTYABR", "Октябрьский", "Oktyabrsky", "Октябрь району", "#93C5FD",
            new (double, double)[]
            {
                (42.918, 74.540), (42.918, 74.595), (42.905, 74.605),
                (42.890, 74.605), (42.880, 74.595), (42.873, 74.580),
                (42.866, 74.575), (42.858, 74.560), (42.858, 74.530),
                (42.880, 74.515), (42.900, 74.525)
            }),
        new("PERVOMAY", "Первомайский", "Pervomaisky", "Первомай району", "#FCA5A5",
            new (double, double)[]
            {
                (42.920, 74.595), (42.925, 74.720), (42.905, 74.745),
                (42.880, 74.745), (42.870, 74.720), (42.865, 74.690),
                (42.870, 74.640), (42.876, 74.612), (42.886, 74.605),
                (42.905, 74.605)
            }),
        new("LENIN", "Ленинский", "Leninsky", "Ленин району", "#86EFAC",
            new (double, double)[]
            {
                (42.866, 74.510), (42.858, 74.530), (42.858, 74.560),
                (42.866, 74.575), (42.870, 74.595), (42.855, 74.610),
                (42.830, 74.605), (42.810, 74.590), (42.795, 74.570),
                (42.790, 74.540), (42.795, 74.510), (42.830, 74.495)
            }),
        new("SVERDLOV", "Свердловский", "Sverdlovsky", "Свердлов району", "#FCD34D",
            new (double, double)[]
            {
                (42.870, 74.595), (42.876, 74.612), (42.870, 74.640),
                (42.865, 74.690), (42.870, 74.720), (42.850, 74.735),
                (42.820, 74.730), (42.795, 74.715), (42.785, 74.685),
                (42.788, 74.640), (42.798, 74.610), (42.815, 74.598),
                (42.840, 74.598), (42.855, 74.610)
            })
    };

    public static async Task SeedAsync(ApplicationDbContext db, ILogger? logger)
    {
        if (await db.Districts.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var spec in Specs)
        {
            var district = new District
            {
                Id = Guid.NewGuid(),
                Code = spec.Code,
                NameRu = spec.NameRu,
                NameEn = spec.NameEn,
                NameKy = spec.NameKy,
                ColorHex = spec.ColorHex,
                CreatedAt = now,
                UpdatedAt = now
            };

            for (var i = 0; i < spec.Vertices.Length; i++)
            {
                var v = spec.Vertices[i];
                district.Boundary.Add(new DistrictBoundaryPoint
                {
                    Id = Guid.NewGuid(),
                    DistrictId = district.Id,
                    SequenceNumber = i,
                    Latitude = v.Lat,
                    Longitude = v.Lng,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            db.Districts.Add(district);
        }

        await db.SaveChangesAsync();
        logger?.LogInformation("Seeded {Count} Bishkek districts", Specs.Length);
    }

    // Rewrites boundary points for all four known districts in place. Used
    // when the polygon spec is improved (e.g. simplified rectangles →
    // irregular shapes) so districts can be reshaped without losing their
    // IDs or AssignedInspectorId values. Returns the number of districts
    // whose boundaries were rewritten.
    // Hand-tuned irregular polygons baked into the seeder. Cheap fallback when
    // a real GeoJSON snapshot isn't available.
    public static Task<int> ReseedBoundariesAsync(
        IApplicationDbContext db,
        IDistrictResolver districtResolver,
        ILogger? logger,
        CancellationToken ct = default)
    {
        var verticesByCode = Specs.ToDictionary(
            s => s.Code,
            s => (IReadOnlyList<(double Lat, double Lng)>)s.Vertices.ToList());
        return ReseedCoreAsync(db, districtResolver, logger, ct,
            sourceLabel: "hand-tuned spec",
            verticesByCode: verticesByCode);
    }

    // Real OSM-derived polygons read from
    // Districts/Snapshots/bishkek-districts.geojson. Validated by
    // GeoJsonDistrictsLoader; if validation fails an exception bubbles before
    // any DB write happens.
    public static async Task<int> ReseedFromGeoJsonAsync(
        IApplicationDbContext db,
        IDistrictResolver districtResolver,
        ILogger? logger,
        CancellationToken ct = default)
    {
        var features = await GeoJsonDistrictsLoader.LoadAsync(ct);
        var verticesByCode = features.ToDictionary(f => f.Code, f => f.Vertices);
        return await ReseedCoreAsync(db, districtResolver, logger, ct,
            sourceLabel: "GeoJSON snapshot",
            verticesByCode: verticesByCode);
    }

    // Single transactional core used by both reseed entry points. Each
    // public method computes the (code → vertex list) dictionary and calls in
    // here; the transactional structure, idempotency, and cache-invalidation
    // logic live in exactly one place.
    //
    // Wraps everything in an explicit transaction so the bulk ExecuteDeletes
    // and the trailing SaveChanges commit or roll back as one unit. The
    // previous shape relied on EF's implicit per-SaveChanges transaction and
    // used per-row tracked deletes (RemoveRange on Include-loaded children),
    // which produced a DbUpdateConcurrencyException ("0 rows affected") on
    // re-run. ExecuteDeleteAsync issues one bulk DELETE per district with no
    // per-row tracking.
    private static async Task<int> ReseedCoreAsync(
        IApplicationDbContext db,
        IDistrictResolver districtResolver,
        ILogger? logger,
        CancellationToken ct,
        string sourceLabel,
        IReadOnlyDictionary<string, IReadOnlyList<(double Lat, double Lng)>> verticesByCode)
    {
        var now = DateTime.UtcNow;
        var rewritten = 0;

        await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            foreach (var (code, vertices) in verticesByCode)
            {
                // Load WITHOUT Include — the boundary points are removed by
                // the bulk delete below, no tracking required.
                var district = await db.Districts
                    .FirstOrDefaultAsync(d => d.Code == code, ct);

                if (district is null)
                {
                    logger?.LogWarning(
                        "Reseed ({Source}): district {Code} not found — call SeedAsync first",
                        sourceLabel, code);
                    continue;
                }

                // Bulk delete — single statement, no tracking, no per-row
                // "expected 1 affected" check. Idempotent: 0 rows is fine.
                await db.DistrictBoundaryPoints
                    .Where(p => p.DistrictId == district.Id)
                    .ExecuteDeleteAsync(ct);

                for (var i = 0; i < vertices.Count; i++)
                {
                    var v = vertices[i];
                    db.DistrictBoundaryPoints.Add(new DistrictBoundaryPoint
                    {
                        Id = Guid.NewGuid(),
                        DistrictId = district.Id,
                        SequenceNumber = i,
                        Latitude = v.Lat,
                        Longitude = v.Lng,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                district.UpdatedAt = now;
                rewritten++;
                logger?.LogInformation(
                    "Reseed ({Source}): rewrote {Count} boundary points for district {Code}",
                    sourceLabel, vertices.Count, code);
            }

            if (rewritten > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // Invalidate the resolver's 15-min in-memory cache ONLY after a
        // successful commit, so a rolled-back reseed doesn't blow away the
        // last known-good cache entry.
        if (rewritten > 0)
        {
            districtResolver.InvalidateCache();
        }
        return rewritten;
    }
}
