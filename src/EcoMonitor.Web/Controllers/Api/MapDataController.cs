using EcoMonitor.Application.Features.AirQuality.Queries.GetStationsForMap;
using EcoMonitor.Application.Features.DumpsiteReports.Public;
using EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersForMap;
using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Helpers;
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

    [HttpGet("air-quality")]
    public async Task<IActionResult> AirQuality()
    {
        var data = await _mediator.Send(new GetStationsForMapQuery());

        var result = data.Select(s =>
        {
            var freshness = FreshnessHelper.Classify(s.MeasuredAt);
            var aqiLevel = s.AqiUs.HasValue
                ? AqiHelper.ClassifyAqiUs(s.AqiUs)
                : AqiHelper.ClassifyPm25(s.Pm25);
            var color = freshness == ReadingFreshness.VeryStale
                ? "#9CA3AF"
                : AqiHelper.GetColorHex(aqiLevel);

            return new
            {
                id = s.Id,
                name = s.Name,
                providerName = s.ProviderName,
                source = s.Source.ToString(),
                latitude = s.Latitude,
                longitude = s.Longitude,
                pm25 = s.Pm25,
                pm10 = s.Pm10,
                temperature = s.Temperature,
                humidity = s.Humidity,
                pressure = s.Pressure,
                aqiUs = s.AqiUs,
                measuredAt = s.MeasuredAt,
                measuredRelative = s.MeasuredAt.HasValue ? DateHelpers.FormatRelative(s.MeasuredAt.Value) : "no data",
                aqiLabel = AqiHelper.GetLabel(aqiLevel),
                color,
                isStale = freshness == ReadingFreshness.Stale || freshness == ReadingFreshness.VeryStale,
                isVeryStale = freshness == ReadingFreshness.VeryStale
            };
        });

        return Ok(result);
    }

    [HttpGet("dumpsites")]
    public async Task<IActionResult> Dumpsites()
    {
        var data = await _mediator.Send(new GetPublicDumpsitesQuery());

        var result = data.Select(d => new
        {
            id = d.Id,
            shortDescription = d.ShortDescription,
            status = d.Status.ToString(),
            statusLabel = d.Status.GetDisplayName(),
            latitude = d.Latitude,
            longitude = d.Longitude,
            createdAt = d.CreatedAt,
            createdRelative = DateHelpers.FormatRelative(d.CreatedAt),
            resolvedAt = d.ResolvedAt,
            photoCount = d.PhotoCount,
            color = d.Status switch
            {
                DumpsiteStatus.Confirmed => "#EF4444",
                DumpsiteStatus.Resolved => "#10B981",
                _ => "#9CA3AF"
            }
        });

        return Ok(result);
    }
}
