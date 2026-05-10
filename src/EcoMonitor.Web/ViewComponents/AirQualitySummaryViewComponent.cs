using EcoMonitor.Application.Features.AirQuality.Queries.GetStationsForMap;
using EcoMonitor.Web.Helpers;
using EcoMonitor.Web.Models.Air;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoMonitor.Web.ViewComponents;

public class AirQualitySummaryViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public AirQualitySummaryViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var stations = await _mediator.Send(new GetStationsForMapQuery());

        var fresh = stations
            .Where(s => s.MeasuredAt.HasValue
                && (DateTime.UtcNow - s.MeasuredAt.Value) < TimeSpan.FromHours(24)
                && s.Pm25.HasValue)
            .ToList();

        double? avgPm25 = fresh.Count > 0 ? fresh.Average(s => s.Pm25!.Value) : null;

        var vm = new AirQualitySummaryViewModel
        {
            AveragePm25 = avgPm25,
            AqiLevel = AqiHelper.ClassifyPm25(avgPm25),
            ActiveStations = fresh.Count,
            TotalStations = stations.Count
        };

        return View(vm);
    }
}
