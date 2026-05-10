using System.Security.Claims;
using EcoMonitor.Application.Features.Sensors.IngestSensorReading;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers.Api;

[ApiController]
[Route("api/v1/sensors")]
public class SensorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SensorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("readings")]
    [Authorize(Policy = "DeviceOnly")]
    public async Task<IActionResult> PostReading(
        [FromBody] SensorReadingDto dto,
        CancellationToken ct)
    {
        var deviceGuidStr = User.FindFirstValue("deviceGuid");
        if (!Guid.TryParse(deviceGuidStr, out var deviceGuid))
        {
            return Unauthorized(new { error = "Invalid token: missing deviceGuid claim" });
        }

        var result = await _mediator.Send(new IngestSensorReadingCommand(deviceGuid, dto), ct);
        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { accepted = true, readingId = result.ReadingId });
    }
}
