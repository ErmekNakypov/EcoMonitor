using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ResolveReport;

public class ResolveReportHandler : IRequestHandler<ResolveReportCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IReportNotificationService _notifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<ResolveReportHandler> _logger;

    public ResolveReportHandler(
        IApplicationDbContext dbContext,
        IReportNotificationService notifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<ResolveReportHandler> logger)
    {
        _dbContext = dbContext;
        _notifications = notifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.Status != DumpsiteStatus.AwaitingVerification)
        {
            throw new DomainException("Only reports awaiting verification can be marked as resolved.");
        }

        // Verifying inspector may differ from the originally assigned inspector
        // (spec: "the verifying inspector may or may not be the original assigned inspector").

        report.Status = DumpsiteStatus.Resolved;
        report.ResolutionNotes = request.Notes;
        report.ResolvedAt = DateTime.UtcNow;
        report.VerifiedAt = DateTime.UtcNow;
        report.VerifiedByInspectorId = request.InspectorId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Inspector {InspectorId} verified+resolved report {ReportId}",
            request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, cancellationToken);
        await _events.LogAsync(report.Id, DumpsiteEventType.MarkedResolved,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: request.Notes, ct: cancellationToken);

        try
        {
            await _notifications.NotifyReportResolvedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue resolution email for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
