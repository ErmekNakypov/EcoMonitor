using EcoMonitor.Application.Features.Analytics.GetAverageAqiByDay;
using EcoMonitor.Application.Features.Analytics.GetDumpsiteReportsByDay;
using EcoMonitor.Application.Features.Analytics.GetDumpsiteStatusBreakdown;
using EcoMonitor.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Analytics")]
public class AnalyticsController : Controller
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Data")]
    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        // Sequential awaits — the three queries share the request-scoped DbContext,
        // which is not safe to use concurrently.
        var reportsByDay = await _mediator.Send(new GetDumpsiteReportsByDayQuery(), cancellationToken);
        var statusBreakdown = await _mediator.Send(new GetDumpsiteStatusBreakdownQuery(), cancellationToken);
        var aqiByDay = await _mediator.Send(new GetAverageAqiByDayQuery(), cancellationToken);

        return Json(new
        {
            reportsByDay = reportsByDay.Select(r => new
            {
                date = r.Date.ToString("yyyy-MM-dd"),
                count = r.Count
            }),
            statusBreakdown = statusBreakdown.Select(s => new
            {
                statusName = s.StatusName,
                count = s.Count,
                colorHex = s.ColorHex
            }),
            aqiByDay = aqiByDay.Select(a => new
            {
                date = a.Date.ToString("yyyy-MM-dd"),
                avgAqiUs = a.AvgAqiUs,
                stationCount = a.StationCount
            })
        });
    }
}
