using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace EcoMonitor.Infrastructure.Telegram;

public sealed class TelegramReportNotificationService : ITelegramNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly BotLocalizer _localizer;
    private readonly ILogger<TelegramReportNotificationService> _logger;

    public TelegramReportNotificationService(
        ApplicationDbContext db,
        IConfiguration configuration,
        BotLocalizer localizer,
        ILogger<TelegramReportNotificationService> logger)
    {
        _db = db;
        _configuration = configuration;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid reportId, ReportStatusNotification kind, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report is null || report.Source != ReportSource.Telegram || !report.TelegramUserId.HasValue)
        {
            return; // Web-submitted reports are reached via email.
        }

        var token = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Telegram:BotToken not set; cannot send report notification");
            return;
        }

        var session = await _db.TelegramUserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TelegramUserId == report.TelegramUserId.Value, ct);
        var lang = session?.Language ?? BotLocalizer.DefaultLanguage;

        var key = kind switch
        {
            ReportStatusNotification.Confirmed         => "notif_report_confirmed",
            ReportStatusNotification.Rejected          => "notif_report_rejected",
            ReportStatusNotification.CleanupStarted    => "notif_cleanup_started",
            ReportStatusNotification.CleanupCompleted  => "notif_cleanup_completed",
            ReportStatusNotification.Resolved          => "notif_report_resolved",
            _ => "notif_report_confirmed"
        };

        var refId = report.Id.ToString()[..8].ToUpperInvariant();
        var message = _localizer.Get(lang, key, refId);

        try
        {
            var bot = new TelegramBotClient(token);
            await bot.SendMessage(report.TelegramUserId.Value, message, cancellationToken: ct);
            _logger.LogInformation(
                "Sent Telegram notification {Kind} for report {ReportId} to user {UserId}",
                kind, reportId, report.TelegramUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send Telegram notification {Kind} for report {ReportId}", kind, reportId);
        }
    }
}
