using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
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
    public static async Task<int> ReseedBoundariesAsync(
        IApplicationDbContext db,
        IDistrictResolver districtResolver,
        ILogger? logger,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rewritten = 0;

        foreach (var spec in Specs)
        {
            var district = await db.Districts
                .Include(d => d.Boundary)
                .FirstOrDefaultAsync(d => d.Code == spec.Code, ct);

            if (district is null)
            {
                logger?.LogWarning(
                    "Reseed: district {Code} not found — call SeedAsync first", spec.Code);
                continue;
            }

            db.DistrictBoundaryPoints.RemoveRange(district.Boundary);
            district.Boundary.Clear();

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

            district.UpdatedAt = now;
            rewritten++;
            logger?.LogInformation(
                "Reseed: rewrote {Count} boundary points for district {Code}",
                spec.Vertices.Length, spec.Code);
        }

        if (rewritten > 0)
        {
            await db.SaveChangesAsync(ct);
            // The resolver caches districts+boundaries for 15 min; without
            // invalidation, the new polygons would not take effect until the
            // next process restart.
            districtResolver.InvalidateCache();
        }
        return rewritten;
    }
}
