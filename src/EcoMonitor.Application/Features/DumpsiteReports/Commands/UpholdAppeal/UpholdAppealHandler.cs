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
    private readonly ILogger<UpholdAppealHandler> _logger;

    public UpholdAppealHandler(
        IApplicationDbContext db,
        IReportNotificationService notifications,
        ILogger<UpholdAppealHandler> logger)
    {
        _db = db;
        _notifications = notifications;
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
