using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Application.Features.Notifications.EmailTemplates;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Email;

public sealed class EmailReportNotificationService : IReportNotificationService
{
    private const string TemplateRoot = "~/Views/EmailTemplates/";

    private readonly IApplicationDbContext _db;
    private readonly IUserLookupService _userLookup;
    private readonly IEmailQueue _queue;
    private readonly IRazorViewRenderer _renderer;
    private readonly ITelegramNotificationService _telegram;
    private readonly ILogger<EmailReportNotificationService> _logger;

    public EmailReportNotificationService(
        IApplicationDbContext db,
        IUserLookupService userLookup,
        IEmailQueue queue,
        IRazorViewRenderer renderer,
        ITelegramNotificationService telegram,
        ILogger<EmailReportNotificationService> logger)
    {
        _db = db;
        _userLookup = userLookup;
        _queue = queue;
        _renderer = renderer;
        _telegram = telegram;
        _logger = logger;
    }

    // Telegram-submitted reports skip the email step inside LoadAsync, so each
    // notification fans out to *both* channels — the one that doesn't apply
    // short-circuits internally.
    private Task FanOutTelegramAsync(Guid reportId, ReportStatusNotification kind, CancellationToken ct) =>
        _telegram.NotifyAsync(reportId, kind, ct);

    public async Task NotifyReportCreatedAsync(Guid reportId, CancellationToken ct = default)
    {
        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, _) = ctx.Value;

        var model = new ReportCreatedEmailModel(
            reporter.FullName,
            report.Id,
            report.Description,
            report.CreatedAt,
            WasAutoConfirmed: report.Status == DumpsiteStatus.Confirmed && report.AutoTriageReason is null,
            AutoTriageReason: report.AutoTriageReason);

        var html = await _renderer.RenderAsync(TemplateRoot + "ReportCreated.cshtml", model);
        var subject = $"EcoMonitor: Report received (ref: {ShortRef(report.Id)})";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "ReportCreated", report.Id, ct);
    }

    public async Task NotifyReportConfirmedAsync(Guid reportId, CancellationToken ct = default)
    {
        await FanOutTelegramAsync(reportId, ReportStatusNotification.Confirmed, ct);

        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, inspector) = ctx.Value;

        var confirmedAt = report.UpdatedAt;
        var model = new ReportConfirmedEmailModel(
            reporter.FullName,
            report.Id,
            inspector?.FullName ?? "an inspector",
            confirmedAt);

        var html = await _renderer.RenderAsync(TemplateRoot + "ReportConfirmed.cshtml", model);
        var subject = "EcoMonitor: Report confirmed and being processed";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "ReportConfirmed", report.Id, ct);
    }

    public async Task NotifyReportRejectedAsync(Guid reportId, CancellationToken ct = default)
    {
        await FanOutTelegramAsync(reportId, ReportStatusNotification.Rejected, ct);

        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, inspector) = ctx.Value;

        var model = new ReportRejectedEmailModel(
            reporter.FullName,
            report.Id,
            inspector?.FullName ?? "an inspector",
            report.ResolutionNotes ?? string.Empty,
            report.ResolvedAt ?? report.UpdatedAt);

        var html = await _renderer.RenderAsync(TemplateRoot + "ReportRejected.cshtml", model);
        var subject = "EcoMonitor: Report could not be confirmed";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "ReportRejected", report.Id, ct);
    }

    public async Task NotifyCleanupStartedAsync(Guid reportId, CancellationToken ct = default)
    {
        await FanOutTelegramAsync(reportId, ReportStatusNotification.CleanupStarted, ct);

        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, _) = ctx.Value;

        var model = new CleanupStartedEmailModel(
            reporter.FullName,
            report.Id,
            report.CleanupStartedAt ?? report.UpdatedAt);

        var html = await _renderer.RenderAsync(TemplateRoot + "CleanupStarted.cshtml", model);
        var subject = "EcoMonitor: Cleanup is now in progress";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "CleanupStarted", report.Id, ct);
    }

    public async Task NotifyCleanupCompletedAsync(Guid reportId, CancellationToken ct = default)
    {
        await FanOutTelegramAsync(reportId, ReportStatusNotification.CleanupCompleted, ct);

        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, _) = ctx.Value;

        var model = new CleanupCompletedEmailModel(
            reporter.FullName,
            report.Id,
            report.CleanupCompletedAt ?? report.UpdatedAt,
            report.CleanupNotes ?? string.Empty);

        var html = await _renderer.RenderAsync(TemplateRoot + "CleanupCompleted.cshtml", model);
        var subject = "EcoMonitor: Cleanup completed, awaiting verification";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "CleanupCompleted", report.Id, ct);
    }

    public async Task NotifyReportResolvedAsync(Guid reportId, CancellationToken ct = default)
    {
        await FanOutTelegramAsync(reportId, ReportStatusNotification.Resolved, ct);

        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, inspector) = ctx.Value;

        var model = new ReportResolvedEmailModel(
            reporter.FullName,
            report.Id,
            inspector?.FullName ?? "an inspector",
            report.ResolutionNotes ?? string.Empty,
            report.ResolvedAt ?? report.UpdatedAt);

        var html = await _renderer.RenderAsync(TemplateRoot + "ReportResolved.cshtml", model);
        var subject = "EcoMonitor: Dumpsite resolved - thank you!";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "ReportResolved", report.Id, ct);
    }

    private async Task<(DumpsiteReport Report, Application.Common.Models.UserSummaryDto Reporter, Application.Common.Models.UserSummaryDto? Inspector)?> LoadAsync(
        Guid reportId, CancellationToken ct)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report is null)
        {
            _logger.LogWarning("Report {ReportId} not found while preparing notification email", reportId);
            return null;
        }

        if (report.Source == ReportSource.Telegram)
        {
            _logger.LogInformation("Skipping email for Telegram-submitted report {ReportId}", reportId);
            return null;
        }

        if (!report.ReporterId.HasValue)
        {
            _logger.LogInformation("Skipping email for report {ReportId} with no reporter", reportId);
            return null;
        }

        var reporter = await _userLookup.GetByIdAsync(report.ReporterId.Value, ct);
        if (reporter is null || string.IsNullOrWhiteSpace(reporter.Email))
        {
            _logger.LogInformation(
                "Skipping email for report {ReportId}: reporter {ReporterId} not found or has no email",
                reportId, report.ReporterId);
            return null;
        }

        var inspector = report.AssignedInspectorId.HasValue
            ? await _userLookup.GetByIdAsync(report.AssignedInspectorId.Value, ct)
            : null;

        return (report, reporter, inspector);
    }

    private static string ShortRef(Guid id) => id.ToString()[..8].ToUpperInvariant();
}
