using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers;

[AllowAnonymous]
[Route("Map")]
public class MapController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
