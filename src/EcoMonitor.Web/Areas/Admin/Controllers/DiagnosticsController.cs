using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
    private readonly IStringLocalizer<DiagnosticsController> _localizer;

    public DiagnosticsController(
        IAirQualityIngestionRunner runner,
        IContainerImportService containerImporter,
        IApplicationDbContext db,
        IDistrictResolver districts,
        ILogger<DiagnosticsController> logger,
        IStringLocalizer<DiagnosticsController> localizer)
    {
        _runner = runner;
        _containerImporter = containerImporter;
        _db = db;
        _districts = districts;
        _logger = logger;
        _localizer = localizer;
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
            // Provider names like "OpenAQ" are literal product names — they
            // stay in English regardless of locale. Only the "no providers
            // contributed" fallback phrase + the failure-suffix label are
            // routed through the controller bundle.
            var breakdown = string.Join(" + ",
                result.ProviderResults
                    .Where(p => p.Saved > 0)
                    .Select(p => $"{p.Saved} {p.ProviderName}"));
            if (string.IsNullOrEmpty(breakdown)) breakdown = _localizer["IngestionNoProviders"].Value;

            var failed = result.ProviderResults.Where(p => p.Error is not null).ToList();
            var failedSuffix = failed.Count == 0
                ? string.Empty
                : _localizer["IngestionFailedSuffixFormat",
                    string.Join(", ", failed.Select(p => $"{p.ProviderName} ({p.Error})"))].Value;

            TempData["SuccessMessage"] = _localizer["IngestionSuccessFormat",
                result.TotalReadingsSaved, breakdown, result.DuplicatesSkipped, failedSuffix].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer["IngestionErrorFormat", result.Error].Value;
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

        TempData["SuccessMessage"] = _localizer["BackfillReportsSuccessFormat", matched, unassigned.Count].Value;
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

        TempData["SuccessMessage"] = _localizer["BackfillContainersSuccessFormat", matched, unassigned.Count].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ReseedDistrictBoundaries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReseedDistrictBoundaries(CancellationToken ct)
    {
        var rewritten = await BishkekDistrictsSeeder.ReseedBoundariesAsync(
            _db, _districts, _logger, ct);
        TempData["SuccessMessage"] = _localizer["ReseedBoundariesSuccessFormat", rewritten].Value;
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
            TempData["SuccessMessage"] = _localizer["ReseedFromGeoJsonSuccessFormat", rewritten].Value;
        }
        catch (FileNotFoundException ex)
        {
            TempData["ErrorMessage"] = _localizer["GeoJsonMissingErrorFormat", ex.Message].Value;
        }
        catch (InvalidDataException ex)
        {
            TempData["ErrorMessage"] = _localizer["GeoJsonInvalidErrorFormat", ex.Message].Value;
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
            var sourceLabel = result.Source == "live"
                ? _localizer["OsmSourceLive"].Value
                : _localizer["OsmSourceSnapshot"].Value;
            TempData["SuccessMessage"] = _localizer["OsmImportSuccessFormat",
                sourceLabel, result.Created, result.Updated, result.Skipped, result.TotalFetched].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer["OsmImportErrorFormat", result.Error].Value;
        }

        return RedirectToAction(nameof(Index));
    }
}
