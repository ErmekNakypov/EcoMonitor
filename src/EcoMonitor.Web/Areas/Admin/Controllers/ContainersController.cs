using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Features.WasteContainers.Commands.CreateWasteContainer;
using EcoMonitor.Application.Features.WasteContainers.Commands.DeleteWasteContainer;
using EcoMonitor.Application.Features.WasteContainers.Commands.UpdateWasteContainer;
using EcoMonitor.Application.Features.WasteContainers.Queries.GetContainerById;
using EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersList;
using EcoMonitor.Application.Features.WasteContainers.Queries.GetNextContainerCode;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Admin.Containers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Containers")]
public class ContainersController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContainersController> _logger;

    public ContainersController(IMediator mediator, ILogger<ContainersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, ContainerType? type, ContainerStatus? status, int page = 1)
    {
        var result = await _mediator.Send(new GetContainersListQuery(search, type, status, page, 20));

        var model = new ContainerListViewModel
        {
            SearchQuery = search,
            TypeFilter = type,
            StatusFilter = status,
            Items = result.Items.Select(c => new ContainerListItemViewModel
            {
                Id = c.Id,
                Code = c.Code,
                Address = c.Address,
                Type = c.Type,
                Status = c.Status,
                Capacity = c.Capacity,
                InstalledAt = c.InstalledAt
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return View(model);
    }

    [HttpGet("Map")]
    public IActionResult Map() => View();

    [HttpGet("Create")]
    public IActionResult Create() => View(new CreateContainerViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContainerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _mediator.Send(new CreateWasteContainerCommand(
                model.Code,
                model.Address,
                model.Latitude,
                model.Longitude,
                model.Type,
                model.Capacity,
                model.InstalledAt));

            TempData["SuccessMessage"] = $"Container {model.Code} created.";
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            foreach (var failure in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, failure.ErrorMessage);
            }
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var details = await _mediator.Send(new GetContainerByIdQuery(id));
        if (details is null)
        {
            return NotFound();
        }

        var model = new EditContainerViewModel
        {
            Id = details.Id,
            Code = details.Code,
            Address = details.Address,
            Latitude = details.Latitude,
            Longitude = details.Longitude,
            Type = details.Type,
            Capacity = details.Capacity,
            Status = details.Status,
            InstalledAt = details.InstalledAt,
            IsImported = details.IsImported,
            OsmId = details.OsmId
        };

        return View(model);
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditContainerViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _mediator.Send(new UpdateWasteContainerCommand(
                model.Id,
                model.Code,
                model.Address,
                model.Latitude,
                model.Longitude,
                model.Type,
                model.Capacity,
                model.Status,
                model.InstalledAt));

            TempData["SuccessMessage"] = $"Container {model.Code} updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            foreach (var failure in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, failure.ErrorMessage);
            }
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost("Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteWasteContainerCommand(id));
            TempData["SuccessMessage"] = "Container marked as removed.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("NextCode")]
    public async Task<IActionResult> NextCode()
    {
        var code = await _mediator.Send(new GetNextContainerCodeQuery());
        return Json(new { code });
    }
}
