using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Triage;

// Rule-based auto-triage tuned for Bishkek municipal use.
// Mirrors the model used by civic-tech platforms like Moscow "Nash Gorod"
// and SeeClickFix: most reports are obvious and route straight to cleanup;
// only suspicious ones go through Inspector review.
public sealed class BishkekAutoTriageService : IAutoTriageService
{
    private const double BishkekMinLat = 42.78;
    private const double BishkekMaxLat = 42.95;
    private const double BishkekMinLng = 74.45;
    private const double BishkekMaxLng = 74.75;
    private const int MinDescriptionLength = 15;
    private const double DuplicateRadiusMeters = 25.0;

    private static readonly DumpsiteStatus[] ActiveStatuses =
    {
        DumpsiteStatus.Confirmed,
        DumpsiteStatus.CleanupInProgress,
        DumpsiteStatus.AwaitingVerification
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<BishkekAutoTriageService> _logger;

    public BishkekAutoTriageService(ApplicationDbContext db, ILogger<BishkekAutoTriageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TriageDecision> EvaluateAsync(DumpsiteReport report, CancellationToken ct = default)
    {
        // Rule 1: at least one photo
        if (report.PhotoPaths is null || report.PhotoPaths.Count == 0)
        {
            return new TriageDecision(false,
                "No photos attached. Inspector review needed to assess validity.");
        }

        // Rule 2: coordinates inside Bishkek bounds
        if (report.Latitude < BishkekMinLat || report.Latitude > BishkekMaxLat ||
            report.Longitude < BishkekMinLng || report.Longitude > BishkekMaxLng)
        {
            return new TriageDecision(false,
                $"Coordinates ({report.Latitude:F4}, {report.Longitude:F4}) outside Bishkek bounds. Inspector review needed.");
        }

        // Rule 3: description has at least MinDescriptionLength meaningful chars
        if (string.IsNullOrWhiteSpace(report.Description) ||
            report.Description.Trim().Length < MinDescriptionLength)
        {
            return new TriageDecision(false,
                $"Description too short (less than {MinDescriptionLength} characters). Inspector review needed.");
        }

        // Rule 4: no active duplicate within DuplicateRadiusMeters.
        // We over-fetch (all currently-active reports) and apply Haversine in
        // memory — for a single-city dataset the count is small enough that
        // this is cheaper than computing distance in SQL.
        var nearby = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => ActiveStatuses.Contains(r.Status))
            .Where(r => r.Id != report.Id)
            .Select(r => new { r.Id, r.Latitude, r.Longitude })
            .ToListAsync(ct);

        foreach (var existing in nearby)
        {
            var distance = HaversineDistanceMeters(
                report.Latitude, report.Longitude,
                existing.Latitude, existing.Longitude);
            if (distance <= DuplicateRadiusMeters)
            {
                return new TriageDecision(false,
                    $"Possible duplicate of existing report at {distance:F0}m distance. Inspector review needed.");
            }
        }

        _logger.LogInformation(
            "Report {ReportId} passed auto-triage at ({Lat}, {Lng})",
            report.Id, report.Latitude, report.Longitude);
        return new TriageDecision(true, null);
    }

    // Haversine formula in metres. Accurate to ~1m at city scale.
    private static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double EarthRadiusMeters = 6371000.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
