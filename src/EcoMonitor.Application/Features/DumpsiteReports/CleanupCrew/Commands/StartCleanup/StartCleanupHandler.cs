using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.StartCleanup;

public class StartCleanupHandler : IRequestHandler<StartCleanupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IReportNotificationService _notifications;
    private readonly ILogger<StartCleanupHandler> _logger;

    public StartCleanupHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IReportNotificationService notifications,
        ILogger<StartCleanupHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Unit> Handle(StartCleanupCommand request, CancellationToken cancellationToken)
    {
        if (request.BeforePhotos is null || request.BeforePhotos.Count == 0)
        {
            throw new DomainException("At least one before-cleanup photo is required.");
        }

        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.CleanupCrewId != request.CleanupUserId)
        {
            throw new ForbiddenException("You are not the assigned cleanup crew for this report.");
        }

        if (report.Status != DumpsiteStatus.Confirmed)
        {
            throw new DomainException("Cleanup can only start from the Confirmed state.");
        }

        foreach (var photo in request.BeforePhotos)
        {
            var path = await _fileStorage.SaveAsync(photo, "cleanup", cancellationToken);
            _dbContext.DumpsiteCleanupPhotos.Add(new DumpsiteCleanupPhoto
            {
                ReportId = report.Id,
                FilePath = path,
                Type = CleanupPhotoType.BeforeCleanup,
                UploadedByUserId = request.CleanupUserId
            });
        }

        report.Status = DumpsiteStatus.CleanupInProgress;
        report.CleanupStartedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleanup user {UserId} started cleanup for report {ReportId} with {Count} before-photo(s)",
            request.CleanupUserId, report.Id, request.BeforePhotos.Count);

        try
        {
            await _notifications.NotifyCleanupStartedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue cleanup-started notification for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
