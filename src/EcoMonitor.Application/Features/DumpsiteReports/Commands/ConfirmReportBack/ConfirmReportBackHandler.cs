using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.ConfirmReportBack;

public class ConfirmReportBackHandler : IRequestHandler<ConfirmReportBackCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<ConfirmReportBackHandler> _logger;

    public ConfirmReportBackHandler(
        IApplicationDbContext db,
        IRoleNotificationService roleNotifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<ConfirmReportBackHandler> logger)
    {
        _db = db;
        _roleNotifications = roleNotifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(ConfirmReportBackCommand request, CancellationToken ct)
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
            throw new DomainException("Only flagged reports can be confirmed back.");
        }

        var stamp = $"[After cleanup flag review {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] {notes}";
        report.InspectorObservations = string.IsNullOrWhiteSpace(report.InspectorObservations)
            ? stamp
            : report.InspectorObservations + "\n\n" + stamp;
        report.Status = DumpsiteStatus.Confirmed;
        report.AssignedInspectorId = request.InspectorId;
        // CleanupCrewId stays — same crew re-checks.

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inspector {InspectorId} confirmed flagged report {ReportId} back to the same crew",
            request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, ct);
        await _events.LogAsync(report.Id, DumpsiteEventType.Confirmed,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: "Returned to same crew after flag review: " + notes, ct: ct);

        try
        {
            await _roleNotifications.NotifyCleanupCrewOfReturnedReportAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify cleanup crew of returned report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
