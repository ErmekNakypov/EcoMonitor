using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.ReassignToAnotherCrew;

public class ReassignToAnotherCrewHandler : IRequestHandler<ReassignToAnotherCrewCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<ReassignToAnotherCrewHandler> _logger;

    public ReassignToAnotherCrewHandler(
        IApplicationDbContext db,
        IRoleNotificationService roleNotifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<ReassignToAnotherCrewHandler> logger)
    {
        _db = db;
        _roleNotifications = roleNotifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(ReassignToAnotherCrewCommand request, CancellationToken ct)
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
            throw new DomainException("Only flagged reports can be reassigned.");
        }

        var stamp = $"[Reassigned after flag {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] {notes}";
        report.InspectorObservations = string.IsNullOrWhiteSpace(report.InspectorObservations)
            ? stamp
            : report.InspectorObservations + "\n\n" + stamp;

        var excludedCrewId = report.CleanupFlaggedByCrewId;

        report.Status = DumpsiteStatus.Confirmed;
        report.CleanupCrewId = null;
        report.ReassignCount += 1;
        report.AssignedInspectorId = request.InspectorId;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inspector {InspectorId} reassigned flagged report {ReportId} to another crew",
            request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, ct);
        await _events.LogAsync(report.Id, DumpsiteEventType.ReassignedToAnotherCrew,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: notes, ct: ct);

        try
        {
            await _roleNotifications.NotifyCleanupCrewOfReassignedReportAsync(
                report.Id,
                excludedCrewId,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify cleanup crew of reassigned report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
