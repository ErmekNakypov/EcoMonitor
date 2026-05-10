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
            TempData["SuccessMessage"] = $"Ingestion ok: {result.ReadingsSaved} new readings ({result.DuplicatesSkipped} duplicates skipped) from {result.Provider}.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Ingestion problem: {result.Error}";
        }

        return RedirectToAction(nameof(Index));
    }
}
