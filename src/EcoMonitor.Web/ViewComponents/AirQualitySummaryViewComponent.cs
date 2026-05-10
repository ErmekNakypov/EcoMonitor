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
                && s.AqiUs.HasValue)
            .ToList();

        double? avgAqi = fresh.Count > 0 ? fresh.Average(s => s.AqiUs!.Value) : null;

        var vm = new AirQualitySummaryViewModel
        {
            AverageAqiUs = avgAqi,
            AqiLevel = AqiHelper.ClassifyAqiUs(avgAqi),
            ActiveStations = fresh.Count,
            TotalStations = stations.Count
        };

        return View(vm);
    }
}
