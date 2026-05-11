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
    private readonly IReportNotificationService _notifications;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly ILogger<SubmitDumpsiteReportHandler> _logger;

    public SubmitDumpsiteReportHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IReportNotificationService notifications,
        IRoleNotificationService roleNotifications,
        ILogger<SubmitDumpsiteReportHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
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

        _dbContext.DumpsiteReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Dumpsite report {ReportId} submitted by {ReporterId} with {PhotoCount} photo(s)",
            report.Id, request.ReporterId, savedPaths.Count);

        try
        {
            await _notifications.NotifyReportCreatedAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue creation email for report {ReportId}", report.Id);
        }

        try
        {
            await _roleNotifications.NotifyInspectorsOfNewReportAsync(report.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify inspectors of new report {ReportId}", report.Id);
        }

        return report.Id;
    }
}
