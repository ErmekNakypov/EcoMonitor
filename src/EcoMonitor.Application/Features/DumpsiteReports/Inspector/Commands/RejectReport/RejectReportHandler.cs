using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectReport;

public class RejectReportHandler : IRequestHandler<RejectReportCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IReportNotificationService _notifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<RejectReportHandler> _logger;

    public RejectReportHandler(
        IApplicationDbContext dbContext,
        IReportNotificationService notifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<RejectReportHandler> logger)
    {
        _dbContext = dbContext;
        _notifications = notifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(RejectReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.AssignedInspectorId != request.InspectorId)
        {
            throw new ForbiddenException("You are not the assigned inspector for this report.");
        }

        if (report.Status != DumpsiteStatus.InReview)
        {
            throw new DomainException("Only reports in review can be rejected.");
        }

        report.Status = DumpsiteStatus.Rejected;
        report.ResolutionNotes = request.Reason;
        report.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inspector {InspectorId} rejected report {ReportId}", request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, cancellationToken);
        await _events.LogAsync(report.Id, DumpsiteEventType.Rejected,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: request.Reason, ct: cancellationToken);

        try
        {
            await _notifications.NotifyReportRejectedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue rejection email for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
