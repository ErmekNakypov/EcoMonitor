using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.AppealReport;

public class AppealReportHandler : IRequestHandler<AppealReportCommand, Unit>
{
    public static readonly TimeSpan AppealWindow = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IReportNotificationService _notifications;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly ILogger<AppealReportHandler> _logger;

    public AppealReportHandler(
        IApplicationDbContext db,
        IFileStorageService fileStorage,
        IReportNotificationService notifications,
        IRoleNotificationService roleNotifications,
        ILogger<AppealReportHandler> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _roleNotifications = roleNotifications;
        _logger = logger;
    }

    public async Task<Unit> Handle(AppealReportCommand request, CancellationToken ct)
    {
        var reason = (request.AppealReason ?? string.Empty).Trim();
        if (reason.Length < 10 || reason.Length > 500)
        {
            throw new DomainException("Appeal reason must be 10–500 characters.");
        }
        if (request.Photos is { Count: > 5 })
        {
            throw new DomainException("At most 5 appeal photos may be attached.");
        }

        var report = await _db.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.ReporterId != request.CitizenId)
        {
            throw new ForbiddenException("Only the original reporter may appeal this resolution.");
        }

        if (report.Status != DumpsiteStatus.Resolved)
        {
            throw new DomainException("Only resolved reports can be appealed.");
        }

        if (!report.ResolvedAt.HasValue
            || DateTime.UtcNow - report.ResolvedAt.Value > AppealWindow)
        {
            throw new DomainException("The 7-day appeal window for this report has closed.");
        }

        foreach (var photo in (IEnumerable<UploadedPhotoDto>?)request.Photos ?? Array.Empty<UploadedPhotoDto>())
        {
            var path = await _fileStorage.SaveAsync(photo, "appeals", ct);
            _db.DumpsiteAppealPhotos.Add(new DumpsiteAppealPhoto
            {
                ReportId = report.Id,
                FilePath = path,
                UploadedByCitizenId = request.CitizenId,
                UploadedAt = DateTime.UtcNow
            });
        }

        report.Status = DumpsiteStatus.Appealed;
        report.AppealedAt = DateTime.UtcNow;
        report.AppealReason = reason;
        // Clear any prior review fields in case this is a re-appeal flow later.
        report.AppealReviewedAt = null;
        report.AppealReviewedByInspectorId = null;
        report.AppealResolutionNotes = null;
        report.AppealOutcome = null;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Citizen {CitizenId} appealed report {ReportId} with {PhotoCount} photo(s)",
            request.CitizenId, report.Id, request.Photos?.Count ?? 0);

        try
        {
            await _notifications.NotifyAppealFiledAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue appeal-filed email for report {ReportId}", report.Id);
        }

        try
        {
            await _roleNotifications.NotifyInspectorsOfAppealAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify inspectors of appeal for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
