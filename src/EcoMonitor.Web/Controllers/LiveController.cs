using EcoMonitor.Web.Models.Live;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Controllers;

[AllowAnonymous]
[Route("Live")]
public class LiveController : Controller
{
    // Demo targets for the thesis showcase. Change these to point the page at
    // different devices/containers — the page itself is generic.
    private static readonly Guid DemoAirStationId =
        new("3f1cfdbf-4879-4591-b1ff-0762605eb15d"); // ESP32-F53AD6 (ESP32-air-01)

    private static readonly Guid DemoContainerId =
        new("3fabdee1-fd41-4a92-9f98-17e26e82b118"); // C-00001 (HC-SR04 rangefinder)

    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new LiveDashboardViewModel
        {
            AirStationId = DemoAirStationId,
            AirDeviceName = "ESP32-air-01",
            ContainerId = DemoContainerId,
            ContainerCode = "C-00001"
        };
        return View(model);
    }
}
