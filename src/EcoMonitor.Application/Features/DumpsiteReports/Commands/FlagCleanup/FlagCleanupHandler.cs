using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.FlagCleanup;

public class FlagCleanupHandler : IRequestHandler<FlagCleanupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IRoleNotificationService _roleNotifications;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<FlagCleanupHandler> _logger;

    public FlagCleanupHandler(
        IApplicationDbContext db,
        IFileStorageService fileStorage,
        IRoleNotificationService roleNotifications,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<FlagCleanupHandler> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _roleNotifications = roleNotifications;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(FlagCleanupCommand request, CancellationToken ct)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (!FlagCleanupReasons.All.Contains(reason))
        {
            throw new DomainException("Flag reason must be one of the predefined values.");
        }

        var notes = (request.AdditionalNotes ?? string.Empty).Trim();
        if (reason == FlagCleanupReasons.Other)
        {
            if (notes.Length < 10 || notes.Length > 500)
            {
                throw new DomainException("When reason is 'Other', additional notes must be 10–500 characters.");
            }
        }

        if (request.Photos is null || request.Photos.Count == 0)
        {
            throw new DomainException("At least one evidence photo is required when flagging a report.");
        }
        if (request.Photos.Count > 5)
        {
            throw new DomainException("At most 5 evidence photos may be attached.");
        }

        var report = await _db.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.CleanupCrewId != request.CleanupCrewId)
        {
            throw new ForbiddenException("Only the assigned cleanup crew member can flag this report.");
        }

        if (report.Status != DumpsiteStatus.Confirmed && report.Status != DumpsiteStatus.CleanupInProgress)
        {
            throw new DomainException("Only Confirmed or CleanupInProgress reports can be flagged.");
        }

        foreach (var photo in request.Photos)
        {
            var path = await _fileStorage.SaveAsync(photo, "flags", ct);
            _db.DumpsiteCleanupPhotos.Add(new DumpsiteCleanupPhoto
            {
                ReportId = report.Id,
                FilePath = path,
                Type = CleanupPhotoType.FlagEvidence,
                UploadedByUserId = request.CleanupCrewId
            });
        }

        report.Status = DumpsiteStatus.FlaggedByCleanupCrew;
        report.CleanupRejectionReason = reason;
        report.CleanupRejectionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        report.CleanupFlaggedAt = DateTime.UtcNow;
        report.CleanupFlaggedByCrewId = request.CleanupCrewId;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Cleanup crew {CrewId} flagged report {ReportId}: {Reason}",
            request.CleanupCrewId, report.Id, reason);

        var actor = await _userLookup.GetByIdAsync(request.CleanupCrewId, ct);
        var noteForTimeline = FlagCleanupReasons.Display(reason)
            + (string.IsNullOrWhiteSpace(notes) ? string.Empty : $" — {notes}");
        await _events.LogAsync(report.Id, DumpsiteEventType.CleanupFlagged,
            request.CleanupCrewId, "CleanupCrew", actor?.FullName ?? "Cleanup crew",
            notes: noteForTimeline, ct: ct);

        try
        {
            await _roleNotifications.NotifyInspectorsOfFlaggedReportAsync(report.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify inspectors of flagged report {ReportId}", report.Id);
        }

        return Unit.Value;
    }
}
