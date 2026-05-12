using EcoMonitor.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers.Api;

[Route("api/districts")]
[ApiController]
[AllowAnonymous]
public class DistrictsApiController : ControllerBase
{
    private readonly IDistrictResolver _resolver;

    public DistrictsApiController(IDistrictResolver resolver)
    {
        _resolver = resolver;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var districts = await _resolver.GetAllAsync(ct);
        var payload = districts.Select(d => new
        {
            id = d.Id,
            code = d.Code,
            name_ru = d.NameRu,
            name_en = d.NameEn,
            color = d.ColorHex,
            boundary = d.Boundary
                .OrderBy(b => b.SequenceNumber)
                .Select(b => new[] { b.Latitude, b.Longitude })
                .ToList()
        });
        return Ok(payload);
    }
}
