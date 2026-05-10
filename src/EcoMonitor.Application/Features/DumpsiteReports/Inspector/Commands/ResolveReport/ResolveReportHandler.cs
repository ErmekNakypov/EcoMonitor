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
    private readonly ILogger<ResolveReportHandler> _logger;

    public ResolveReportHandler(
        IApplicationDbContext dbContext,
        IReportNotificationService notifications,
        ILogger<ResolveReportHandler> logger)
    {
        _dbContext = dbContext;
        _notifications = notifications;
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

        if (report.AssignedInspectorId != request.InspectorId)
        {
            throw new ForbiddenException("You are not the assigned inspector for this report.");
        }

        if (report.Status != DumpsiteStatus.Confirmed)
        {
            throw new DomainException("Only confirmed reports can be marked as resolved.");
        }

        report.Status = DumpsiteStatus.Resolved;
        report.ResolutionNotes = request.Notes;
        report.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inspector {InspectorId} resolved report {ReportId}", request.InspectorId, report.Id);

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
