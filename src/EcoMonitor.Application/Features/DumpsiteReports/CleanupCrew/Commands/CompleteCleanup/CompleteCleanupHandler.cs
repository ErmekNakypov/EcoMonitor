using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.CompleteCleanup;

public class CompleteCleanupHandler : IRequestHandler<CompleteCleanupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IReportNotificationService _notifications;
    private readonly ILogger<CompleteCleanupHandler> _logger;

    public CompleteCleanupHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IReportNotificationService notifications,
        ILogger<CompleteCleanupHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Unit> Handle(CompleteCleanupCommand request, CancellationToken cancellationToken)
    {
        if (request.AfterPhotos is null || request.AfterPhotos.Count == 0)
        {
            throw new DomainException("At least one after-cleanup photo is required.");
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

        if (report.Status != DumpsiteStatus.CleanupInProgress)
        {
            throw new DomainException("Cleanup can only be completed from the In-progress state.");
        }

        foreach (var photo in request.AfterPhotos)
        {
            var path = await _fileStorage.SaveAsync(photo, "cleanup", cancellationToken);
            _dbContext.DumpsiteCleanupPhotos.Add(new DumpsiteCleanupPhoto
            {
                ReportId = report.Id,
                FilePath = path,
                Type = CleanupPhotoType.AfterCleanup,
                UploadedByUserId = request.CleanupUserId
            });
        }

        report.Status = DumpsiteStatus.AwaitingVerification;
        report.CleanupCompletedAt = DateTime.UtcNow;
        report.CleanupNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleanup user {UserId} completed cleanup for report {ReportId} with {Count} after-photo(s)",
            request.CleanupUserId, report.Id, request.AfterPhotos.Count);

        try
        {
            await _notifications.NotifyCleanupCompletedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue cleanup-completed notification for report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
