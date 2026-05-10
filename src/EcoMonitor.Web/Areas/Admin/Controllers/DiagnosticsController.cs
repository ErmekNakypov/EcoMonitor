using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Diagnostics")]
public class DiagnosticsController : Controller
{
    private readonly IAirQualityIngestionRunner _runner;

    public DiagnosticsController(IAirQualityIngestionRunner runner)
    {
        _runner = runner;
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
}
