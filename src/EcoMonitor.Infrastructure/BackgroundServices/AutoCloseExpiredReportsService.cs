using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.BackgroundServices;

public sealed class AutoCloseExpiredReportsService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CycleInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan AppealWindow = TimeSpan.FromDays(7);

    private readonly IServiceProvider _services;
    private readonly ILogger<AutoCloseExpiredReportsService> _logger;

    public AutoCloseExpiredReportsService(
        IServiceProvider services,
        ILogger<AutoCloseExpiredReportsService> logger)
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
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Auto-close cycle failed");
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

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = scope.ServiceProvider.GetRequiredService<IReportEventLogger>();

        var cutoff = DateTime.UtcNow - AppealWindow;

        var expired = await db.DumpsiteReports
            .Where(r => r.Status == DumpsiteStatus.Resolved
                && r.ResolvedAt != null
                && r.ResolvedAt < cutoff)
            .ToListAsync(ct);

        if (expired.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var report in expired)
        {
            report.Status = DumpsiteStatus.Closed;
            report.ClosedAt = now;
        }

        await db.SaveChangesAsync(ct);

        foreach (var report in expired)
        {
            await events.LogAsync(report.Id, DumpsiteEventType.AutoClosed,
                null, "System", "Auto-close service",
                notes: "7-day appeal window passed", ct: ct);
        }

        _logger.LogInformation(
            "Auto-closed {Count} reports past the {Days}-day appeal window",
            expired.Count, AppealWindow.TotalDays);
    }
}
