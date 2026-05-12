using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.ConfirmReportBack;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.DismissAppeal;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.ReassignToAnotherCrew;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.RejectFlaggedReport;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.UpholdAppeal;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectCleanup;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ResolveReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.TakeReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetAppealsQueue;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetFlaggedReports;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportForInspector;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportQueue;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetVerificationQueue;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Web.Models.Inspector;
using EcoMonitor.Web.Models.Reports;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers;

[Authorize(Roles = RoleNames.Inspector)]
[Route("Inspector")]
public class InspectorController : Controller
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<InspectorController> _logger;

    public InspectorController(
        IMediator mediator,
        UserManager<ApplicationUser> userManager,
        ILogger<InspectorController> logger)
    {
        _mediator = mediator;
        _userManager = userManager;
        _logger = logger;
    }

    private Guid CurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpGet("Queue")]
    public async Task<IActionResult> Queue(string? search, string? sortBy, string? source, int page = 1)
    {
        var result = await _mediator.Send(new GetReportQueueQuery(page, 20, search, sortBy, source));

        var model = new QueueListViewModel
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        ViewBag.Search = search;
        ViewBag.Sort = sortBy;
        ViewBag.Source = source;
        return View(model);
    }

    [HttpGet("Flagged")]
    public async Task<IActionResult> Flagged(string? search, string? sortBy, int page = 1)
    {
        var result = await _mediator.Send(new GetFlaggedReportsQuery(page, 20, search, sortBy));
        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.Search = search;
        ViewBag.Sort = sortBy;
        return View(result.Items);
    }

    [HttpPost("Reports/{id:guid}/RejectFlagged")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectFlagged(Guid id, string decisionNotes)
    {
        try
        {
            await _mediator.Send(new RejectFlaggedReportCommand(id, CurrentUserId(), decisionNotes ?? string.Empty));
            TempData["SuccessMessage"] = "Report rejected. Citizen has been notified.";
        }
        catch (NotFoundException) { return NotFound(); }
        catch (DomainException ex) { TempData["ErrorMessage"] = ex.Message; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage)); }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Reports/{id:guid}/ConfirmBack")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBack(Guid id, string decisionNotes)
    {
        try
        {
            await _mediator.Send(new ConfirmReportBackCommand(id, CurrentUserId(), decisionNotes ?? string.Empty));
            TempData["SuccessMessage"] = "Report returned to the same cleanup crew.";
        }
        catch (NotFoundException) { return NotFound(); }
        catch (DomainException ex) { TempData["ErrorMessage"] = ex.Message; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage)); }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Reports/{id:guid}/Reassign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(Guid id, string decisionNotes)
    {
        try
        {
            await _mediator.Send(new ReassignToAnotherCrewCommand(id, CurrentUserId(), decisionNotes ?? string.Empty));
            TempData["SuccessMessage"] = "Report reassigned to another crew.";
        }
        catch (NotFoundException) { return NotFound(); }
        catch (DomainException ex) { TempData["ErrorMessage"] = ex.Message; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage)); }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Verification")]
    public async Task<IActionResult> Verification(string? search, string? sortBy, int page = 1)
    {
        var result = await _mediator.Send(new GetVerificationQueueQuery(page, 20, search, sortBy));
        ViewBag.Search = search;
        ViewBag.Sort = sortBy;
        return View(result);
    }

    [HttpGet("Appeals")]
    public async Task<IActionResult> Appeals(string? search, string? sortBy, int page = 1)
    {
        var result = await _mediator.Send(new GetAppealsQueueQuery(page, 20, search, sortBy));
        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.Search = search;
        ViewBag.Sort = sortBy;
        return View(result.Items);
    }

    [HttpPost("Reports/{id:guid}/UpholdAppeal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpholdAppeal(Guid id, string resolutionNotes)
    {
        try
        {
            await _mediator.Send(new UpholdAppealCommand(id, CurrentUserId(), resolutionNotes ?? string.Empty));
            TempData["SuccessMessage"] = "Appeal upheld. Report returned to the cleanup queue.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Reports/{id:guid}/DismissAppeal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissAppeal(Guid id, string resolutionNotes)
    {
        try
        {
            await _mediator.Send(new DismissAppealCommand(id, CurrentUserId(), resolutionNotes ?? string.Empty));
            TempData["SuccessMessage"] = "Appeal dismissed. Report remains resolved.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("MyReports")]
    public async Task<IActionResult> MyReports(string? tab, int page = 1)
    {
        var t = string.IsNullOrWhiteSpace(tab) ? "active" : tab.ToLowerInvariant();
        if (t != "active" && t != "completed" && t != "all") t = "active";

        var result = await _mediator.Send(new GetMyAssignedReportsQuery(CurrentUserId(), t, page, 20));

        var model = new MyAssignedViewModel
        {
            Tab = t,
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            ActiveCount = result.ActiveCount,
            CompletedCount = result.CompletedCount,
            OverallCount = result.OverallCount
        };

        return View(model);
    }

    [HttpGet("Reports/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var dto = await _mediator.Send(new GetReportForInspectorQuery(id));
        if (dto is null)
        {
            return NotFound();
        }

        var currentId = CurrentUserId();
        var model = new ReportViewModel
        {
            Id = dto.Id,
            Description = dto.Description,
            Status = dto.Status,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            PhotoPaths = dto.PhotoPaths,
            ReporterId = dto.ReporterId,
            ReporterEmail = dto.ReporterEmail,
            ReporterFullName = dto.ReporterFullName,
            AssignedInspectorId = dto.AssignedInspectorId,
            AssignedInspectorEmail = dto.AssignedInspectorEmail,
            ResolutionNotes = dto.ResolutionNotes,
            ResolvedAt = dto.ResolvedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Source = dto.Source,
            TelegramUserName = dto.TelegramUserName,
            InspectorObservations = dto.InspectorObservations,
            InspectionPhotos = dto.InspectionPhotos,
            CleanupBeforePhotos = dto.CleanupBeforePhotos,
            CleanupAfterPhotos = dto.CleanupAfterPhotos,
            CleanupCrewId = dto.CleanupCrewId,
            CleanupCrewName = dto.CleanupCrewName,
            CleanupStartedAt = dto.CleanupStartedAt,
            CleanupCompletedAt = dto.CleanupCompletedAt,
            CleanupNotes = dto.CleanupNotes,
            AutoTriageReason = dto.AutoTriageReason,
            TelegramUserId = dto.TelegramUserId,
            ReporterTotalReports = dto.ReporterTotalReports,
            ReporterPendingReports = dto.ReporterPendingReports,
            ReporterResolvedReports = dto.ReporterResolvedReports,
            ReporterRejectedReports = dto.ReporterRejectedReports,
            AppealedAt = dto.AppealedAt,
            AppealReason = dto.AppealReason,
            AppealPhotos = dto.AppealPhotos,
            AppealReviewedAt = dto.AppealReviewedAt,
            AppealResolutionNotes = dto.AppealResolutionNotes,
            AppealOutcome = dto.AppealOutcome,
            ClosedAt = dto.ClosedAt,
            IsAssignedToCurrentUser = dto.AssignedInspectorId == currentId,
            CleanupRejectionReason = dto.CleanupRejectionReason,
            CleanupRejectionNotes = dto.CleanupRejectionNotes,
            CleanupFlaggedAt = dto.CleanupFlaggedAt,
            CleanupFlaggedByCrewName = dto.CleanupFlaggedByCrewName,
            ReassignCount = dto.ReassignCount,
            FlagEvidencePhotos = dto.FlagEvidencePhotos,
            Events = ReportEventViewModelMapper.MapStaff(dto.Events)
        };

        return View(model);
    }

    [HttpPost("Reports/{id:guid}/Take")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(Guid id)
    {
        try
        {
            await _mediator.Send(new TakeReportCommand(id, CurrentUserId()));
            TempData["SuccessMessage"] = "Report taken. It is now in your queue.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Queue));
        }
    }

    [HttpPost("Reports/{id:guid}/Confirm")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Confirm(Guid id, ConfirmInputModel model)
    {
        if (model.Photos is null || model.Photos.Count == 0)
        {
            TempData["ErrorMessage"] = "Attach at least one inspection photo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var uploaded = new List<UploadedPhotoDto>(model.Photos.Count);
        foreach (var file in model.Photos)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            uploaded.Add(new UploadedPhotoDto(file.FileName, file.ContentType, stream.ToArray()));
        }

        try
        {
            await _mediator.Send(new ConfirmReportCommand(
                id,
                CurrentUserId(),
                model.Observations ?? string.Empty,
                uploaded));
            TempData["SuccessMessage"] = "Report confirmed. It is now in the cleanup queue.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Reports/{id:guid}/RejectCleanup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectCleanup(Guid id, RejectCleanupInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Rework reason must be at least 10 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _mediator.Send(new RejectCleanupCommand(id, CurrentUserId(), model.Reason));
            TempData["SuccessMessage"] = "Report sent back to the cleanup crew for rework.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Reports/{id:guid}/Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, RejectInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Reason must be at least 10 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _mediator.Send(new RejectReportCommand(id, CurrentUserId(), model.Reason));
            TempData["SuccessMessage"] = "Report rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Reports/{id:guid}/Resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, ResolveInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Resolution notes must be at least 10 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _mediator.Send(new ResolveReportCommand(id, CurrentUserId(), model.Notes));
            TempData["SuccessMessage"] = "Report marked as resolved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
