using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.RejectFlaggedReport;

public class RejectFlaggedReportHandler : IRequestHandler<RejectFlaggedReportCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IReportNotificationService _notifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<RejectFlaggedReportHandler> _logger;

    public RejectFlaggedReportHandler(
        IApplicationDbContext db,
        IReportNotificationService notifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<RejectFlaggedReportHandler> logger)
    {
        _db = db;
        _notifications = notifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(RejectFlaggedReportCommand request, CancellationToken ct)
    {
        var notes = (request.DecisionNotes ?? string.Empty).Trim();
        if (notes.Length < 10 || notes.Length > 500)
        {
            throw new DomainException("Decision notes must be 10–500 characters.");
        }

        var report = await _db.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.Status != DumpsiteStatus.FlaggedByCleanupCrew)
        {
            throw new DomainException("Only flagged reports can be rejected via this flow.");
        }

        report.Status = DumpsiteStatus.Rejected;
        report.ResolutionNotes = notes;
        report.ResolvedAt = DateTime.UtcNow;
        // Record the deciding inspector — overrides any previously-assigned
        // inspector since this is the final decision-maker.
        report.AssignedInspectorId = request.InspectorId;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inspector {InspectorId} rejected flagged report {ReportId}",
            request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, ct);
        await _events.LogAsync(report.Id, DumpsiteEventType.Rejected,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: notes, ct: ct);

        try
        {
            await _notifications.NotifyReportRejectedAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue rejection email for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
