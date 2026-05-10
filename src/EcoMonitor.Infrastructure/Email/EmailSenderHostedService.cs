using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoMonitor.Infrastructure.Email;

public sealed class EmailSenderHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 10;

    private readonly IServiceProvider _serviceProvider;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSenderHostedService> _logger;

    public EmailSenderHostedService(
        IServiceProvider serviceProvider,
        IOptions<EmailOptions> options,
        ILogger<EmailSenderHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email sender hosted service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email sender worker loop");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;

        var pending = await db.EmailMessages
            .Where(m => m.Status == EmailStatus.Pending
                        && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var msg in pending)
        {
            msg.AttemptCount++;
            var success = await sender.TrySendAsync(msg, ct);

            if (success)
            {
                msg.Status = EmailStatus.Sent;
                msg.SentAt = DateTime.UtcNow;
                msg.NextAttemptAt = null;
                msg.LastError = null;
            }
            else if (msg.AttemptCount >= _options.MaxRetryAttempts)
            {
                msg.Status = EmailStatus.Failed;
                msg.NextAttemptAt = null;
                msg.LastError = "Max retry attempts exceeded";
                _logger.LogError(
                    "Email permanently failed after {Attempts} attempts: {Subject} to {To}",
                    msg.AttemptCount, msg.Subject, msg.ToAddress);
            }
            else
            {
                var delayMinutes = _options.RetryDelayMinutes * msg.AttemptCount;
                msg.NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                msg.LastError = $"SMTP send failed, will retry at {msg.NextAttemptAt:HH:mm}";
                _logger.LogInformation(
                    "Email send failed, retry {Attempt}/{Max} scheduled for {Time}",
                    msg.AttemptCount, _options.MaxRetryAttempts, msg.NextAttemptAt);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
