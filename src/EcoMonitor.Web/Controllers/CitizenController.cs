using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.AppealReport;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;
using EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;
using EcoMonitor.Application.Features.DumpsiteReports.Queries.GetReportDetails;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Web.Models.Citizen;
using EcoMonitor.Web.Models.Reports;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers;

[Authorize(Roles = RoleNames.Citizen)]
[Route("Citizen")]
public class CitizenController : Controller
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private const int MaxPhotos = 5;

    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CitizenController> _logger;

    public CitizenController(
        IMediator mediator,
        UserManager<ApplicationUser> userManager,
        ILogger<CitizenController> logger)
    {
        _mediator = mediator;
        _userManager = userManager;
        _logger = logger;
    }

    private Guid CurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpGet("Reports")]
    public async Task<IActionResult> Reports(string? tab, int page = 1)
    {
        var t = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLowerInvariant();
        if (t != "active" && t != "resolved" && t != "rejected" && t != "all") t = "all";

        var result = await _mediator.Send(new GetMyReportsQuery(CurrentUserId(), t, page, 10));

        var model = new ReportListViewModel
        {
            Tab = t,
            Reports = result.Items.Select(r => new ReportListItemViewModel
            {
                Id = r.Id,
                ShortDescription = r.Description.Length > 120
                    ? r.Description[..120] + "…"
                    : r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                FirstPhotoPath = r.PhotoPaths.FirstOrDefault(),
                Lat = r.Latitude,
                Lng = r.Longitude
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            ActiveCount = result.ActiveCount,
            ResolvedCount = result.ResolvedCount,
            RejectedCount = result.RejectedCount,
            OverallCount = result.OverallCount
        };

        return View(model);
    }

    [HttpGet("Reports/Create")]
    public IActionResult Create()
    {
        return View(new CreateReportViewModel());
    }

    [HttpPost("Reports/Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Create(CreateReportViewModel model, IFormFileCollection? photos)
    {
        photos ??= new FormFileCollection();

        if (photos.Count > MaxPhotos)
        {
            ModelState.AddModelError(string.Empty, $"You can attach at most {MaxPhotos} photos.");
        }

        foreach (var file in photos)
        {
            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError(string.Empty, $"{file.FileName}: only JPEG, PNG, or WEBP images are allowed.");
            }

            if (file.Length > MaxPhotoBytes)
            {
                ModelState.AddModelError(string.Empty, $"{file.FileName}: file is larger than 5 MB.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploaded = new List<UploadedPhotoDto>(photos.Count);
        foreach (var file in photos)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            uploaded.Add(new UploadedPhotoDto(file.FileName, file.ContentType, stream.ToArray()));
        }

        try
        {
            var reportId = await _mediator.Send(new SubmitDumpsiteReportCommand(
                CurrentUserId(),
                model.Description,
                model.Latitude,
                model.Longitude,
                uploaded));

            _logger.LogInformation("Citizen {Email} submitted report {ReportId}", User.Identity?.Name, reportId);
            TempData["SuccessMessage"] = "Report submitted. Thank you for helping keep Bishkek clean.";
            return RedirectToAction(nameof(Reports));
        }
        catch (ValidationException ex)
        {
            foreach (var failure in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, failure.ErrorMessage);
            }
            return View(model);
        }
    }

    [HttpGet("Reports/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var details = await _mediator.Send(new GetReportDetailsQuery(id, CurrentUserId()));
        if (details is null)
        {
            return NotFound();
        }

        var model = new ReportDetailsViewModel
        {
            Id = details.Id,
            Description = details.Description,
            Status = details.Status,
            Latitude = details.Latitude,
            Longitude = details.Longitude,
            PhotoPaths = details.PhotoPaths,
            InspectionPhotos = details.InspectionPhotos,
            BeforeCleanupPhotos = details.BeforeCleanupPhotos,
            AfterCleanupPhotos = details.AfterCleanupPhotos,
            ResolutionNotes = details.ResolutionNotes,
            ResolvedAt = details.ResolvedAt,
            CreatedAt = details.CreatedAt,
            UpdatedAt = details.UpdatedAt,
            AppealedAt = details.AppealedAt,
            AppealReason = details.AppealReason,
            AppealPhotos = details.AppealPhotos,
            AppealReviewedAt = details.AppealReviewedAt,
            AppealResolutionNotes = details.AppealResolutionNotes,
            AppealOutcome = details.AppealOutcome,
            ClosedAt = details.ClosedAt,
            CleanupCrewName = details.CleanupCrewName,
            CleanupCompletedAt = details.CleanupCompletedAt,
            VerifiedByInspectorName = details.VerifiedByInspectorName,
            Events = ReportEventViewModelMapper.MapCitizen(details.Events)
        };

        return View(model);
    }

    [HttpPost("Reports/{id:guid}/Appeal")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Appeal(Guid id, string appealReason, IFormFileCollection? appealPhotos)
    {
        appealPhotos ??= new FormFileCollection();

        if (appealPhotos.Count > MaxPhotos)
        {
            TempData["ErrorMessage"] = $"You can attach at most {MaxPhotos} photos.";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var file in appealPhotos)
        {
            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                TempData["ErrorMessage"] = $"{file.FileName}: only JPEG, PNG, or WEBP images are allowed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (file.Length > MaxPhotoBytes)
            {
                TempData["ErrorMessage"] = $"{file.FileName}: file is larger than 5 MB.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        var uploaded = new List<UploadedPhotoDto>(appealPhotos.Count);
        foreach (var file in appealPhotos)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            uploaded.Add(new UploadedPhotoDto(file.FileName, file.ContentType, stream.ToArray()));
        }

        try
        {
            await _mediator.Send(new AppealReportCommand(id, CurrentUserId(), appealReason ?? string.Empty, uploaded));
            _logger.LogInformation("Citizen {Email} appealed report {ReportId}", User.Identity?.Name, id);
            TempData["SuccessMessage"] = "Appeal submitted. An inspector will review it.";
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
