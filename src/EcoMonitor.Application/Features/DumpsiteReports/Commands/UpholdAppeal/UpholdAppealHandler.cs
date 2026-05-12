using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.UpholdAppeal;

public class UpholdAppealHandler : IRequestHandler<UpholdAppealCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IReportNotificationService _notifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<UpholdAppealHandler> _logger;

    public UpholdAppealHandler(
        IApplicationDbContext db,
        IReportNotificationService notifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<UpholdAppealHandler> logger)
    {
        _db = db;
        _notifications = notifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpholdAppealCommand request, CancellationToken ct)
    {
        var notes = (request.ResolutionNotes ?? string.Empty).Trim();
        if (notes.Length < 10 || notes.Length > 1000)
        {
            throw new DomainException("Inspector notes must be 10–1000 characters.");
        }

        var report = await _db.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.Status != DumpsiteStatus.Appealed)
        {
            throw new DomainException("Only appealed reports can be reviewed.");
        }

        // Upholding an appeal sends the report back to the cleanup crew for
        // rework. CleanupNotes is left alone (it's the crew's own log);
        // AppealResolutionNotes carries the inspector's reason. The rework
        // counter + ReworkStartedAt let the UI explain "attempt #N, returned
        // by citizen appeal" without scanning text.
        report.Status = DumpsiteStatus.CleanupInProgress;
        report.AppealReviewedAt = DateTime.UtcNow;
        report.AppealReviewedByInspectorId = request.InspectorId;
        report.AppealResolutionNotes = notes;
        report.AppealOutcome = AppealOutcome.Upheld;

        report.CleanupCompletedAt = null;
        report.ReworkCount++;
        report.ReworkStartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inspector {InspectorId} upheld appeal for report {ReportId}",
            request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, ct);
        var actorName = actor?.FullName ?? "Inspector";
        await _events.LogAsync(report.Id, DumpsiteEventType.AppealUpheld,
            request.InspectorId, "Inspector", actorName,
            notes: notes, ct: ct);
        // Upheld appeal immediately starts a rework cycle (status flips to
        // CleanupInProgress on the same line above), so emit ReworkStarted
        // as a system event right after so the timeline reads as a chain.
        await _events.LogAsync(report.Id, DumpsiteEventType.ReworkStarted,
            null, "System", "Workflow",
            notes: $"Rework #{report.ReworkCount} after upheld appeal",
            ct: ct);

        try
        {
            await _notifications.NotifyAppealUpheldAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue appeal-upheld email for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
