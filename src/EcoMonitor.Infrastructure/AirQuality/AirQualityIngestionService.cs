using EcoMonitor.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.AirQuality;

public sealed class AirQualityIngestionService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CycleInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceProvider _services;
    private readonly ILogger<AirQualityIngestionService> _logger;

    public AirQualityIngestionService(IServiceProvider services, ILogger<AirQualityIngestionService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IAirQualityIngestionRunner>();
                await runner.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ingestion cycle failed");
            }

            try
            {
                await Task.Delay(CycleInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
