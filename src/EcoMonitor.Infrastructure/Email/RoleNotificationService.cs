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
                    url);

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
