using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.DismissAppeal;

public class DismissAppealHandler : IRequestHandler<DismissAppealCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IReportNotificationService _notifications;
    private readonly ILogger<DismissAppealHandler> _logger;

    public DismissAppealHandler(
        IApplicationDbContext db,
        IReportNotificationService notifications,
        ILogger<DismissAppealHandler> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Unit> Handle(DismissAppealCommand request, CancellationToken ct)
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

        // Dismissing returns the report to Resolved. The 7-day appeal window
        // doesn't reset — the original ResolvedAt stays, so the next
        // auto-close pass picks it up at the normal time.
        report.Status = DumpsiteStatus.Resolved;
        report.AppealReviewedAt = DateTime.UtcNow;
        report.AppealReviewedByInspectorId = request.InspectorId;
        report.AppealResolutionNotes = notes;
        report.AppealOutcome = AppealOutcome.Dismissed;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inspector {InspectorId} dismissed appeal for report {ReportId}",
            request.InspectorId, report.Id);

        try
        {
            await _notifications.NotifyAppealDismissedAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue appeal-dismissed email for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
