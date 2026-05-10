using EcoMonitor.Application.Features.DumpsiteReports.Admin.GetAllReports;
using EcoMonitor.Application.Features.DumpsiteReports.Admin.GetReportForAdmin;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Admin.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Reports")]
public class ReportsController : Controller
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DumpsiteStatus? status, int page = 1)
    {
        var result = await _mediator.Send(new GetAllReportsQuery(status, page, 20));

        var model = new AdminReportListViewModel
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var dto = await _mediator.Send(new GetReportForAdminQuery(id));
        if (dto is null)
        {
            return NotFound();
        }

        var model = new AdminReportViewModel
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
            ReporterRegisteredAt = dto.ReporterRegisteredAt,
            AssignedInspectorId = dto.AssignedInspectorId,
            AssignedInspectorEmail = dto.AssignedInspectorEmail,
            AssignedInspectorFullName = dto.AssignedInspectorFullName,
            ResolutionNotes = dto.ResolutionNotes,
            ResolvedAt = dto.ResolvedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Source = dto.Source,
            TelegramUserName = dto.TelegramUserName
        };

        return View(model);
    }
}
