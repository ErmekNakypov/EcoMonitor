using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Application.Features.Notifications.EmailTemplates;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoMonitor.Infrastructure.Email;

public sealed class RoleNotificationService : IRoleNotificationService
{
    private const string TemplateRoot = "~/Views/EmailTemplates/";

    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailQueue _queue;
    private readonly IRazorViewRenderer _renderer;
    private readonly LinkGenerator _linkGenerator;
    private readonly string _baseUrl;
    private readonly ILogger<RoleNotificationService> _logger;

    public RoleNotificationService(
        IApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailQueue queue,
        IRazorViewRenderer renderer,
        LinkGenerator linkGenerator,
        IOptions<AppOptions> appOptions,
        ILogger<RoleNotificationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _queue = queue;
        _renderer = renderer;
        _linkGenerator = linkGenerator;
        _baseUrl = (appOptions.Value.BaseUrl ?? "http://localhost:5108").TrimEnd('/');
        _logger = logger;
    }

    public async Task NotifyInspectorsOfNewReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Inspector notification: report {ReportId} not found", reportId);
            return;
        }

        var inspectors = await _userManager.GetUsersInRoleAsync(RoleNames.Inspector);
        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: null, controller: "Inspector");

        foreach (var inspector in inspectors.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)))
        {
            try
            {
                var model = new InspectorNewAssignmentEmailModel(
                    inspector.FullName,
                    report.Id,
                    preview,
                    report.CreatedAt,
                    url,
                    AutoTriageReason: report.AutoTriageReason);

                var html = await _renderer.RenderAsync(
                    TemplateRoot + "InspectorNewAssignment.cshtml", model);

                await _queue.EnqueueAsync(
                    inspector.Email!,
                    inspector.FullName,
                    "New report assigned for review - EcoMonitor",
                    html,
                    "InspectorNewAssignment",
                    report.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to enqueue inspector-assignment email for {InspectorEmail}", inspector.Email);
            }
        }
    }

    public async Task NotifyCleanupCrewOfNewTaskAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Cleanup-crew notification: report {ReportId} not found", reportId);
            return;
        }

        var crew = await _userManager.GetUsersInRoleAsync(RoleNames.CleanupCrew);
        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: "CleanupCrew", controller: "Reports");

        foreach (var member in crew.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)))
        {
            try
            {
                var model = new CleanupCrewNewTaskEmailModel(
                    member.FullName,
                    report.Id,
                    preview,
                    report.Latitude,
                    report.Longitude,
                    url);

                var html = await _renderer.RenderAsync(
                    TemplateRoot + "CleanupCrewNewTask.cshtml", model);

                await _queue.EnqueueAsync(
                    member.Email!,
                    member.FullName,
                    "New cleanup task available - EcoMonitor",
                    html,
                    "CleanupCrewNewTask",
                    report.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to enqueue cleanup-crew-task email for {CrewEmail}", member.Email);
            }
        }
    }

    public async Task NotifyInspectorsOfAppealAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Appeal notification: report {ReportId} not found", reportId);
            return;
        }

        var inspectors = await _userManager.GetUsersInRoleAsync(RoleNames.Inspector);
        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: null, controller: "Inspector");

        foreach (var inspector in inspectors.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)))
        {
            try
            {
                var model = new InspectorAppealReceivedEmailModel(
                    inspector.FullName,
                    report.Id,
                    preview,
                    report.AppealReason ?? string.Empty,
                    report.AppealedAt ?? DateTime.UtcNow,
                    url);

                var html = await _renderer.RenderAsync(
                    TemplateRoot + "InspectorAppealReceived.cshtml", model);

                await _queue.EnqueueAsync(
                    inspector.Email!,
                    inspector.FullName,
                    "EcoMonitor: A citizen appealed a closed report",
                    html,
                    "InspectorAppealReceived",
                    report.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to enqueue inspector appeal email for {InspectorEmail}", inspector.Email);
            }
        }
    }

    public async Task NotifyInspectorsOfFlaggedReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Flagged-report notification: report {ReportId} not found", reportId);
            return;
        }

        var crewName = "Cleanup crew";
        if (report.CleanupFlaggedByCrewId.HasValue)
        {
            var crew = await _userManager.FindByIdAsync(report.CleanupFlaggedByCrewId.Value.ToString());
            if (crew is not null) crewName = crew.FullName;
        }

        var reasonDisplay = EcoMonitor.Application.Features.DumpsiteReports.Commands.FlagCleanup
            .FlagCleanupReasons.Display(report.CleanupRejectionReason);

        var inspectors = await _userManager.GetUsersInRoleAsync(RoleNames.Inspector);
        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: null, controller: "Inspector");

        foreach (var inspector in inspectors.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)))
        {
            try
            {
                var model = new InspectorReportFlaggedEmailModel(
                    inspector.FullName,
                    report.Id,
                    preview,
                    crewName,
                    reasonDisplay,
                    report.CleanupRejectionNotes,
                    report.CleanupFlaggedAt ?? DateTime.UtcNow,
                    url);

                var html = await _renderer.RenderAsync(
                    TemplateRoot + "InspectorReportFlagged.cshtml", model);

                await _queue.EnqueueAsync(
                    inspector.Email!,
                    inspector.FullName,
                    "EcoMonitor: Cleanup crew flagged a report",
                    html,
                    "InspectorReportFlagged",
                    report.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to enqueue flagged-report email for {InspectorEmail}", inspector.Email);
            }
        }
    }

    public async Task NotifyCleanupCrewOfReturnedReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null || !report.CleanupCrewId.HasValue)
        {
            _logger.LogWarning("Returned-report notification: report {ReportId} not found or no crew", reportId);
            return;
        }

        var member = await _userManager.FindByIdAsync(report.CleanupCrewId.Value.ToString());
        if (member is null || !member.IsActive || string.IsNullOrEmpty(member.Email))
        {
            return;
        }

        var inspectorName = "Inspector";
        if (report.AssignedInspectorId.HasValue)
        {
            var insp = await _userManager.FindByIdAsync(report.AssignedInspectorId.Value.ToString());
            if (insp is not null) inspectorName = insp.FullName;
        }

        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: "CleanupCrew", controller: "Reports");
        var inspectorNotes = report.InspectorObservations ?? string.Empty;

        try
        {
            var model = new CleanupCrewReportReturnedEmailModel(
                member.FullName, report.Id, preview, inspectorName, inspectorNotes, url);
            var html = await _renderer.RenderAsync(
                TemplateRoot + "CleanupCrewReportReturned.cshtml", model);

            await _queue.EnqueueAsync(
                member.Email,
                member.FullName,
                "EcoMonitor: Inspector reviewed your flag — please re-check",
                html,
                "CleanupCrewReportReturned",
                report.Id,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue cleanup-returned email for {CrewEmail}", member.Email);
        }
    }

    public async Task NotifyCleanupCrewOfReassignedReportAsync(
        Guid reportId, Guid? excludedCrewId, CancellationToken ct = default)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Reassigned-report notification: report {ReportId} not found", reportId);
            return;
        }

        var inspectorName = "Inspector";
        if (report.AssignedInspectorId.HasValue)
        {
            var insp = await _userManager.FindByIdAsync(report.AssignedInspectorId.Value.ToString());
            if (insp is not null) inspectorName = insp.FullName;
        }

        var crew = await _userManager.GetUsersInRoleAsync(RoleNames.CleanupCrew);
        var preview = Truncate(report.Description, 200);
        var url = BuildReportUrl(report.Id, area: "CleanupCrew", controller: "Reports");
        var inspectorNotes = report.InspectorObservations ?? string.Empty;

        foreach (var member in crew.Where(u =>
            u.IsActive && !string.IsNullOrEmpty(u.Email)
            && (excludedCrewId is null || u.Id != excludedCrewId.Value)))
        {
            try
            {
                var model = new CleanupCrewReportReassignedEmailModel(
                    member.FullName, report.Id, preview, inspectorName,
                    inspectorNotes, report.ReassignCount, url);

                var html = await _renderer.RenderAsync(
                    TemplateRoot + "CleanupCrewReportReassigned.cshtml", model);

                await _queue.EnqueueAsync(
                    member.Email!,
                    member.FullName,
                    "EcoMonitor: Report available for pickup (previously flagged)",
                    html,
                    "CleanupCrewReportReassigned",
                    report.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to enqueue cleanup-reassigned email for {CrewEmail}", member.Email);
            }
        }
    }

    // LinkGenerator works without an HTTP request, unlike IUrlHelper.Action,
    // which requires an ActionContext + routing data. The result is a relative
    // path; we prepend the configured AppOptions.BaseUrl so the link is
    // clickable from inside an email client.
    private string BuildReportUrl(Guid reportId, string? area, string controller)
    {
        var values = area is null
            ? (object)new { id = reportId }
            : new { area, id = reportId };

        var path = _linkGenerator.GetPathByAction(
            action: "Details",
            controller: controller,
            values: values);

        if (string.IsNullOrEmpty(path))
        {
            // Fallback: hand-built path. Shouldn't normally happen — routes are
            // registered at startup — but better an ugly link than a crash.
            path = area is null
                ? $"/{controller}/Reports/{reportId}"
                : $"/{area}/{controller}/Details/{reportId}";
        }

        return _baseUrl + path;
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
}
