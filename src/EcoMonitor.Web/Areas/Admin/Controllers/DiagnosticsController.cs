using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Diagnostics")]
public class DiagnosticsController : Controller
{
    private readonly IAirQualityIngestionRunner _runner;
    private readonly IContainerImportService _containerImporter;
    private readonly IApplicationDbContext _db;
    private readonly IDistrictResolver _districts;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IAirQualityIngestionRunner runner,
        IContainerImportService containerImporter,
        IApplicationDbContext db,
        IDistrictResolver districts,
        ILogger<DiagnosticsController> logger)
    {
        _runner = runner;
        _containerImporter = containerImporter;
        _db = db;
        _districts = districts;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("Ingest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunIngest()
    {
        var result = await _runner.RunOnceAsync();
        if (result.Error is null)
        {
            var breakdown = string.Join(" + ",
                result.ProviderResults
                    .Where(p => p.Saved > 0)
                    .Select(p => $"{p.Saved} {p.ProviderName}"));
            if (string.IsNullOrEmpty(breakdown)) breakdown = "no providers contributed";

            var failed = result.ProviderResults.Where(p => p.Error is not null).ToList();
            var failedSuffix = failed.Count == 0
                ? string.Empty
                : $" Failed: {string.Join(", ", failed.Select(p => $"{p.ProviderName} ({p.Error})"))}";

            TempData["SuccessMessage"] =
                $"Ingestion ok: {result.TotalReadingsSaved} new readings ({breakdown}, {result.DuplicatesSkipped} duplicates skipped).{failedSuffix}";
        }
        else
        {
            TempData["ErrorMessage"] = $"Ingestion problem: {result.Error}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("BackfillDistricts")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillDistricts(CancellationToken ct)
    {
        var unassigned = await _db.DumpsiteReports
            .Where(r => r.DistrictId == null)
            .ToListAsync(ct);

        int matched = 0;
        foreach (var report in unassigned)
        {
            var district = await _districts.ResolveAsync(report.Latitude, report.Longitude, ct);
            if (district is null) continue;
            report.DistrictId = district.Id;
            matched++;
        }

        if (matched > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        TempData["SuccessMessage"] =
            $"District backfill complete: {matched} of {unassigned.Count} reports matched a district.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("BackfillContainerDistricts")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillContainerDistricts(CancellationToken ct)
    {
        var unassigned = await _db.WasteContainers
            .Where(c => c.DistrictId == null)
            .ToListAsync(ct);

        int matched = 0;
        foreach (var container in unassigned)
        {
            var district = await _districts.ResolveAsync(container.Latitude, container.Longitude, ct);
            if (district is null) continue;
            container.DistrictId = district.Id;
            matched++;
        }

        if (matched > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        TempData["SuccessMessage"] =
            $"Container district backfill complete: {matched} of {unassigned.Count} containers matched a district.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ReseedDistrictBoundaries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReseedDistrictBoundaries(CancellationToken ct)
    {
        var rewritten = await BishkekDistrictsSeeder.ReseedBoundariesAsync(
            _db, _districts, _logger, ct);
        TempData["SuccessMessage"] =
            $"Reseeded boundary points for {rewritten} of 4 districts. Map polygons and point-in-polygon resolution use the new shapes.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ReseedDistrictBoundariesFromGeoJson")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReseedDistrictBoundariesFromGeoJson(CancellationToken ct)
    {
        try
        {
            var rewritten = await BishkekDistrictsSeeder.ReseedFromGeoJsonAsync(
                _db, _districts, _logger, ct);
            TempData["SuccessMessage"] =
                $"Reseeded boundary points for {rewritten} of 4 districts from the bundled GeoJSON snapshot (real OSM-derived polygons).";
        }
        catch (FileNotFoundException ex)
        {
            TempData["ErrorMessage"] = $"GeoJSON snapshot missing: {ex.Message}";
        }
        catch (InvalidDataException ex)
        {
            TempData["ErrorMessage"] = $"GeoJSON snapshot rejected by validation: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ImportContainers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportContainers()
    {
        var result = await _containerImporter.ImportFromOsmAsync();
        if (result.Error is null)
        {
            var sourceLabel = result.Source == "live" ? "live OSM" : "bundled snapshot";
            TempData["SuccessMessage"] =
                $"OSM import complete (source: {sourceLabel}): {result.Created} created, {result.Updated} updated, {result.Skipped} skipped (out of {result.TotalFetched} fetched).";
        }
        else
        {
            TempData["ErrorMessage"] = $"Import failed: {result.Error}";
        }

        return RedirectToAction(nameof(Index));
    }
}
