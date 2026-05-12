using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;

public class SubmitDumpsiteReportHandler : IRequestHandler<SubmitDumpsiteReportCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IAutoTriageService _triage;
    private readonly IReportNotificationService _notifications;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly ILogger<SubmitDumpsiteReportHandler> _logger;

    public SubmitDumpsiteReportHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IAutoTriageService triage,
        IReportNotificationService notifications,
        IRoleNotificationService roleNotifications,
        ILogger<SubmitDumpsiteReportHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _triage = triage;
        _notifications = notifications;
        _roleNotifications = roleNotifications;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitDumpsiteReportCommand request, CancellationToken cancellationToken)
    {
        var savedPaths = new List<string>(request.Photos.Count);
        foreach (var photo in request.Photos)
        {
            var path = await _fileStorage.SaveAsync(photo, "dumpsites", cancellationToken);
            savedPaths.Add(path);
        }

        var report = new DumpsiteReport
        {
            ReporterId = request.ReporterId,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = DumpsiteStatus.New,
            PhotoPaths = savedPaths
        };

        // Run the report through auto-triage before persisting. Reports that
        // pass every rule skip Inspector review and land directly in the
        // cleanup queue; reports that fail any rule go to InReview with a
        // human-readable reason so the inspector knows what to look at.
        var decision = await _triage.EvaluateAsync(report, cancellationToken);
        if (decision.ShouldAutoConfirm)
        {
            report.Status = DumpsiteStatus.Confirmed;
            report.AutoTriageReason = null;
            report.ConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            report.Status = DumpsiteStatus.InReview;
            report.AutoTriageReason = decision.RejectionReason;
        }

        _dbContext.DumpsiteReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Report {ReportId} submitted by {ReporterId} with {PhotoCount} photo(s) — auto-triage: {Outcome}{Reason}",
            report.Id, request.ReporterId, savedPaths.Count,
            decision.ShouldAutoConfirm ? "confirmed" : "review",
            decision.ShouldAutoConfirm ? string.Empty : $" ({decision.RejectionReason})");

        // Citizen always gets a confirmation email; the template differentiates
        // auto-confirmed vs sent-to-review using report.AutoTriageReason.
        try
        {
            await _notifications.NotifyReportCreatedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue creation email for report {ReportId}", report.Id);
        }

        // Staff notifications are conditional on the routing outcome.
        if (report.Status == DumpsiteStatus.InReview)
        {
            try
            {
                await _roleNotifications.NotifyInspectorsOfNewReportAsync(report.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify inspectors of new report {ReportId}", report.Id);
            }
        }
        else if (report.Status == DumpsiteStatus.Confirmed)
        {
            try
            {
                await _roleNotifications.NotifyCleanupCrewOfNewTaskAsync(report.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify cleanup crew of auto-confirmed report {ReportId}", report.Id);
            }
        }

        return report.Id;
    }
}
