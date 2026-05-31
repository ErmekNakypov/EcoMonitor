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
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly IDistrictResolver _districtResolver;
    private readonly ILogger<SubmitDumpsiteReportHandler> _logger;

    public SubmitDumpsiteReportHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IAutoTriageService triage,
        IReportNotificationService notifications,
        IRoleNotificationService roleNotifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        IDistrictResolver districtResolver,
        ILogger<SubmitDumpsiteReportHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _triage = triage;
        _notifications = notifications;
        _roleNotifications = roleNotifications;
        _userLookup = userLookup;
        _events = events;
        _districtResolver = districtResolver;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitDumpsiteReportCommand request, CancellationToken cancellationToken)
    {
        // Two photo-supply modes:
        //   - Web: caller hands raw UploadedPhotoDto bytes; we save them here.
        //   - Telegram: caller already downloaded the files from the Telegram API
        //     into wwwroot/uploads/dumpsites/ and supplies the resulting relative
        //     paths in PreSavedPhotoPaths. Skip the save loop.
        List<string> savedPaths;
        if (request.PreSavedPhotoPaths is { Count: > 0 })
        {
            savedPaths = request.PreSavedPhotoPaths.ToList();
        }
        else
        {
            savedPaths = new List<string>(request.Photos.Count);
            foreach (var photo in request.Photos)
            {
                var path = await _fileStorage.SaveAsync(photo, "dumpsites", cancellationToken);
                savedPaths.Add(path);
            }
        }

        var report = new DumpsiteReport
        {
            ReporterId = request.ReporterId,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = DumpsiteStatus.New,
            PhotoPaths = savedPaths,
            Source = request.Source,
            TelegramUserId = request.TelegramUserId,
            TelegramUserName = request.TelegramUserName
        };

        // Resolve the district for this report's coordinates so we can route
        // it geographically. Reports outside any district keep DistrictId
        // null and fall back to the broadcast notification path.
        var district = await _districtResolver.ResolveAsync(
            request.Latitude, request.Longitude, cancellationToken);
        report.DistrictId = district?.Id;

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
            // District-targeted auto-assignment: when an inspector owns this
            // district, hand the report straight to them.
            if (district?.AssignedInspectorId is { } inspectorId)
            {
                report.AssignedInspectorId = inspectorId;
            }
        }

        _dbContext.DumpsiteReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Report {ReportId} submitted by {ReporterId} with {PhotoCount} photo(s) — auto-triage: {Outcome}{Reason}",
            report.Id, request.ReporterId, savedPaths.Count,
            decision.ShouldAutoConfirm ? "confirmed" : "review",
            decision.ShouldAutoConfirm ? string.Empty : $" ({decision.RejectionReason})");

        // Audit timeline: report submitted, then auto-triage outcome.
        // For anonymous (Telegram) submissions, fall back to the Telegram handle
        // and skip the user lookup — there is no ApplicationUser to find.
        string citizenName;
        if (request.ReporterId is { } reporterId)
        {
            var reporter = await _userLookup.GetByIdAsync(reporterId, cancellationToken);
            citizenName = reporter?.FullName ?? "Anonymous";
        }
        else
        {
            citizenName = !string.IsNullOrWhiteSpace(request.TelegramUserName)
                ? "@" + request.TelegramUserName
                : "Anonymous Telegram user";
        }
        await _events.LogAsync(report.Id, DumpsiteEventType.ReportSubmitted,
            request.ReporterId, "Citizen", citizenName, ct: cancellationToken);

        if (decision.ShouldAutoConfirm)
        {
            await _events.LogAsync(report.Id, DumpsiteEventType.AutoTriaged,
                null, "System", "Auto-triage system",
                notes: "Passed all triage rules", ct: cancellationToken);
        }
        else
        {
            await _events.LogAsync(report.Id, DumpsiteEventType.SentToReview,
                null, "System", "Auto-triage system",
                notes: decision.RejectionReason, ct: cancellationToken);
        }

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
                if (report.AssignedInspectorId is { } targetInspectorId)
                {
                    // District resolved → notify just the responsible inspector.
                    await _roleNotifications.NotifyInspectorOfNewReviewTaskAsync(
                        report.Id, targetInspectorId, cancellationToken);
                }
                else
                {
                    // Outside all districts (or district has no assigned
                    // inspector) → fall back to broadcasting to every inspector.
                    await _roleNotifications.NotifyInspectorsOfNewReportAsync(
                        report.Id, cancellationToken);
                }
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
