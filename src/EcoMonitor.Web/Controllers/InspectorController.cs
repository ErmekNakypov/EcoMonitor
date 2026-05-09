using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ResolveReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.TakeReport;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetMyAssignedReports;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportForInspector;
using EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportQueue;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Web.Models.Inspector;
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
    public async Task<IActionResult> Queue(int page = 1)
    {
        var result = await _mediator.Send(new GetReportQueueQuery(page, 20));

        var model = new QueueListViewModel
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return View(model);
    }

    [HttpGet("MyReports")]
    public async Task<IActionResult> MyReports(DumpsiteStatus? status, int page = 1)
    {
        var result = await _mediator.Send(new GetMyAssignedReportsQuery(CurrentUserId(), status, page, 20));

        var model = new MyAssignedViewModel
        {
            StatusFilter = status,
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
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
            IsAssignedToCurrentUser = dto.AssignedInspectorId == currentId
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
            TempData["Success"] = "Report taken. It is now in your queue.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Queue));
        }
    }

    [HttpPost("Reports/{id:guid}/Confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            await _mediator.Send(new ConfirmReportCommand(id, CurrentUserId()));
            TempData["Success"] = "Report confirmed.";
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
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Reports/{id:guid}/Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, RejectInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Reason must be at least 10 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _mediator.Send(new RejectReportCommand(id, CurrentUserId(), model.Reason));
            TempData["Success"] = "Report rejected.";
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
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            TempData["Error"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("Reports/{id:guid}/Resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, ResolveInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Resolution notes must be at least 10 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _mediator.Send(new ResolveReportCommand(id, CurrentUserId(), model.Notes));
            TempData["Success"] = "Report marked as resolved.";
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
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            TempData["Error"] = string.Join(' ', ex.Errors.Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
