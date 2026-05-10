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
    private readonly ILogger<EmailReportNotificationService> _logger;

    public EmailReportNotificationService(
        IApplicationDbContext db,
        IUserLookupService userLookup,
        IEmailQueue queue,
        IRazorViewRenderer renderer,
        ILogger<EmailReportNotificationService> logger)
    {
        _db = db;
        _userLookup = userLookup;
        _queue = queue;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task NotifyReportCreatedAsync(Guid reportId, CancellationToken ct = default)
    {
        var ctx = await LoadAsync(reportId, ct);
        if (ctx is null) return;
        var (report, reporter, _) = ctx.Value;

        var model = new ReportCreatedEmailModel(
            reporter.FullName,
            report.Id,
            report.Description,
            report.CreatedAt);

        var html = await _renderer.RenderAsync(TemplateRoot + "ReportCreated.cshtml", model);
        var subject = $"EcoMonitor: Report received (ref: {ShortRef(report.Id)})";
        await _queue.EnqueueAsync(reporter.Email, reporter.FullName, subject, html, "ReportCreated", report.Id, ct);
    }

    public async Task NotifyReportConfirmedAsync(Guid reportId, CancellationToken ct = default)
    {
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

    public async Task NotifyReportResolvedAsync(Guid reportId, CancellationToken ct = default)
    {
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
