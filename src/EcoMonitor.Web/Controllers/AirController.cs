using EcoMonitor.Application.Features.AirQuality.Queries.GetStationDetails;
using EcoMonitor.Web.Models.Air;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers;

[AllowAnonymous]
[Route("Air")]
public class AirController : Controller
{
    private readonly IMediator _mediator;

    public AirController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Station/{id:guid}")]
    public async Task<IActionResult> Station(Guid id)
    {
        var details = await _mediator.Send(new GetStationDetailsQuery(id));
        if (details is null)
        {
            return NotFound();
        }

        return View(new StationDetailsViewModel(details));
    }
}
