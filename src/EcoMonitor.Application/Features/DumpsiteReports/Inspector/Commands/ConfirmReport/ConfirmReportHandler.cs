using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;

public class ConfirmReportHandler : IRequestHandler<ConfirmReportCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IReportNotificationService _notifications;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<ConfirmReportHandler> _logger;

    public ConfirmReportHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IReportNotificationService notifications,
        IRoleNotificationService roleNotifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<ConfirmReportHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _roleNotifications = roleNotifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(ConfirmReportCommand request, CancellationToken cancellationToken)
    {
        if (request.InspectionPhotos is null || request.InspectionPhotos.Count == 0)
        {
            throw new DomainException("At least one inspection photo is required to confirm a report.");
        }

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
            throw new DomainException("Only reports in review can be confirmed.");
        }

        foreach (var photo in request.InspectionPhotos)
        {
            var path = await _fileStorage.SaveAsync(photo, "inspections", cancellationToken);
            _dbContext.DumpsiteInspectionPhotos.Add(new DumpsiteInspectionPhoto
            {
                ReportId = report.Id,
                FilePath = path,
                UploadedByInspectorId = request.InspectorId,
                UploadedAt = DateTime.UtcNow
            });
        }

        report.Status = DumpsiteStatus.Confirmed;
        report.InspectorObservations = string.IsNullOrWhiteSpace(request.Observations)
            ? null
            : request.Observations.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Inspector {InspectorId} confirmed report {ReportId} with {Count} inspection photo(s)",
            request.InspectorId, report.Id, request.InspectionPhotos.Count);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, cancellationToken);
        await _events.LogAsync(report.Id, DumpsiteEventType.Confirmed,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            notes: report.InspectorObservations, ct: cancellationToken);

        try
        {
            await _notifications.NotifyReportConfirmedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue confirmation email for report {ReportId}", report.Id);
        }

        try
        {
            await _roleNotifications.NotifyCleanupCrewOfNewTaskAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify cleanup crew of confirmed report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
