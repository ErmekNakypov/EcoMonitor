using EcoMonitor.Application.Features.Sensors.GetContainerLatestFill;
using EcoMonitor.Application.Features.Sensors.GetRecentAirReadings;
using EcoMonitor.Application.Features.Sensors.GetRecentFillReadings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers.Api;

[Route("api/sensors")]
[ApiController]
[AllowAnonymous]
public class SensorsApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public SensorsApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("air/{stationId:guid}/recent")]
    public async Task<IActionResult> AirRecent(Guid stationId, [FromQuery] int minutes = 60, CancellationToken ct = default)
    {
        var points = await _mediator.Send(new GetRecentAirReadingsQuery(stationId, minutes), ct);
        return Ok(points);
    }

    [HttpGet("containers/{containerId:guid}/recent")]
    public async Task<IActionResult> FillRecent(Guid containerId, [FromQuery] int minutes = 60, CancellationToken ct = default)
    {
        var points = await _mediator.Send(new GetRecentFillReadingsQuery(containerId, minutes), ct);
        return Ok(points);
    }

    [HttpGet("containers/{containerId:guid}/latest")]
    public async Task<IActionResult> FillLatest(Guid containerId, CancellationToken ct = default)
    {
        var snapshot = await _mediator.Send(new GetContainerLatestFillQuery(containerId), ct);
        if (snapshot is null)
        {
            return NotFound();
        }
        return Ok(snapshot);
    }
}
