using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.CompleteCleanup;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.StartCleanup;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.TakeForCleanup;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.FlagCleanup;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetCleanupQueue;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetMyCleanupReports;
using EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetReportForCleanup;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Web.Models.CleanupCrew;
using EcoMonitor.Web.Models.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Areas.CleanupCrew.Controllers;

[Area("CleanupCrew")]
[Authorize(Roles = RoleNames.CleanupCrew)]
[Route("CleanupCrew/Reports")]
public class ReportsController : Controller
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IMediator mediator,
        UserManager<ApplicationUser> userManager,
        ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _userManager = userManager;
        _logger = logger;
    }

    private Guid CurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpGet("Queue")]
    public async Task<IActionResult> Queue(string? search, string? sortBy, string? source, int page = 1, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCleanupQueueQuery(page, 20, search, sortBy, source), ct);
        ViewBag.Search = search;
        ViewBag.Sort = sortBy;
        ViewBag.Source = source;
        return View(result);
    }

    [HttpGet("MyReports")]
    public async Task<IActionResult> MyReports(string? tab, int page = 1, CancellationToken ct = default)
    {
        var t = string.IsNullOrWhiteSpace(tab) ? "active" : tab.ToLowerInvariant();
        if (t != "active" && t != "completed" && t != "all") t = "active";

        var result = await _mediator.Send(new GetMyCleanupReportsQuery(CurrentUserId(), t, page, 20), ct);
        ViewBag.Tab = t;
        return View(result);
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetReportForCleanupQuery(id), ct);
        if (dto is null) return NotFound();
        ViewData["CurrentUserId"] = CurrentUserId();
        ViewData["Events"] = ReportEventViewModelMapper.MapStaff(dto.Events);
        return View(dto);
    }

    [HttpPost("Take/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new TakeForCleanupCommand(id, CurrentUserId()), ct);
            TempData["SuccessMessage"] = "Report taken. Start cleanup when ready.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Queue));
        }
    }

    [HttpPost("StartCleanup/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> StartCleanup(Guid id, StartCleanupInputModel model, CancellationToken ct)
    {
        if (model.BeforePhotos is null || model.BeforePhotos.Count == 0)
        {
            TempData["ErrorMessage"] = "Attach at least one before-cleanup photo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var uploaded = await ToDtos(model.BeforePhotos, ct);

        try
        {
            await _mediator.Send(new StartCleanupCommand(id, CurrentUserId(), uploaded), ct);
            TempData["SuccessMessage"] = "Cleanup started. Upload after-photos when finished.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("CompleteCleanup/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> CompleteCleanup(Guid id, CompleteCleanupInputModel model, CancellationToken ct)
    {
        if (model.AfterPhotos is null || model.AfterPhotos.Count == 0)
        {
            TempData["ErrorMessage"] = "Attach at least one after-cleanup photo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var uploaded = await ToDtos(model.AfterPhotos, ct);

        try
        {
            await _mediator.Send(new CompleteCleanupCommand(
                id, CurrentUserId(), model.Notes ?? string.Empty, uploaded), ct);
            TempData["SuccessMessage"] = "Cleanup completed. The inspector will verify your work.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Flag/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Flag(Guid id, string? Reason, string? AdditionalNotes, List<IFormFile>? Photos, CancellationToken ct)
    {
        if (Photos is null || Photos.Count == 0)
        {
            TempData["ErrorMessage"] = "Attach at least one evidence photo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var uploaded = await ToDtos(Photos, ct);

        try
        {
            await _mediator.Send(new FlagCleanupCommand(
                id, CurrentUserId(), Reason ?? string.Empty, AdditionalNotes, uploaded), ct);
            TempData["SuccessMessage"] = "Report flagged. An inspector will review your evidence.";
            return RedirectToAction(nameof(MyReports));
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private static async Task<IReadOnlyList<UploadedPhotoDto>> ToDtos(List<IFormFile> files, CancellationToken ct)
    {
        var list = new List<UploadedPhotoDto>(files.Count);
        foreach (var f in files)
        {
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms, ct);
            list.Add(new UploadedPhotoDto(f.FileName, f.ContentType, ms.ToArray()));
        }
        return list;
    }
}
