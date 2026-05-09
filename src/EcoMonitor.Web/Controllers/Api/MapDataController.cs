using EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersForMap;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers.Api;

[Route("api/map")]
[ApiController]
[AllowAnonymous]
public class MapDataController : ControllerBase
{
    private readonly IMediator _mediator;

    public MapDataController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("containers")]
    public async Task<IActionResult> Containers()
    {
        var points = await _mediator.Send(new GetContainersForMapQuery());
        return Ok(points);
    }
}
