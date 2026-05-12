using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Common;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetReportForCleanup;

public class GetReportForCleanupHandler : IRequestHandler<GetReportForCleanupQuery, CleanupReportDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly IUserLookupService _userLookup;

    public GetReportForCleanupHandler(IApplicationDbContext db, IUserLookupService userLookup)
    {
        _db = db;
        _userLookup = userLookup;
    }

    public async Task<CleanupReportDto?> Handle(GetReportForCleanupQuery request, CancellationToken ct)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);

        if (report is null) return null;

        var ids = new List<Guid>();
        if (report.ReporterId.HasValue) ids.Add(report.ReporterId.Value);
        if (report.AssignedInspectorId.HasValue) ids.Add(report.AssignedInspectorId.Value);

        IReadOnlyDictionary<Guid, UserSummaryDto> users = ids.Count > 0
            ? await _userLookup.GetByIdsAsync(ids, ct)
            : new Dictionary<Guid, UserSummaryDto>();

        var reporter = report.ReporterId.HasValue ? users.GetValueOrDefault(report.ReporterId.Value) : null;
        var inspector = report.AssignedInspectorId.HasValue
            ? users.GetValueOrDefault(report.AssignedInspectorId.Value)
            : null;

        var inspectionPhotos = await _db.DumpsiteInspectionPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.FilePath)
            .ToListAsync(ct);

        var beforePhotos = await _db.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.BeforeCleanup)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(ct);

        var afterPhotos = await _db.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.AfterCleanup)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(ct);

        var events = await _db.DumpsiteReportEvents
            .AsNoTracking()
            .Where(e => e.ReportId == report.Id)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new ReportEventDto(e.EventType, e.OccurredAt, e.ActorRole, e.ActorDisplayName, e.Notes))
            .ToListAsync(ct);

        var appealPhotos = await _db.DumpsiteAppealPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.FilePath)
            .ToListAsync(ct);

        var flagEvidencePhotos = await _db.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.FlagEvidence)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(ct);

        var stats = await GetReporterStatsAsync(report, ct);

        // The moment a report reached Confirmed status — first inspection photo
        // upload time is the cleanest proxy. UpdatedAt on the report would also
        // be touched by every later state change, so it's unreliable.
        var confirmedAt = await _db.DumpsiteInspectionPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => (DateTime?)p.UploadedAt)
            .FirstOrDefaultAsync(ct);

        return new CleanupReportDto(
            report.Id,
            report.Description,
            report.Status,
            report.Latitude,
            report.Longitude,
            report.PhotoPaths,
            inspectionPhotos,
            beforePhotos,
            afterPhotos,
            appealPhotos,
            report.InspectorObservations,
            report.CleanupNotes,
            report.CleanupCrewId,
            report.CleanupStartedAt,
            report.CleanupCompletedAt,
            report.CreatedAt,
            report.UpdatedAt,
            report.Source,
            reporter?.FullName,
            reporter?.Email,
            report.TelegramUserName,
            report.TelegramUserId,
            stats.Total,
            stats.Pending,
            stats.Resolved,
            stats.Rejected,
            report.AutoTriageReason,
            inspector?.FullName,
            confirmedAt,
            report.ReworkCount,
            report.ReworkStartedAt,
            report.AppealedAt,
            report.AppealReason,
            report.AppealReviewedAt,
            report.AppealResolutionNotes,
            report.AppealOutcome,
            events,
            report.CleanupRejectionReason,
            report.CleanupRejectionNotes,
            report.CleanupFlaggedAt,
            report.CleanupFlaggedByCrewId,
            report.ReassignCount,
            flagEvidencePhotos);
    }

    private async Task<(int Total, int Pending, int Resolved, int Rejected)> GetReporterStatsAsync(
        EcoMonitor.Domain.Entities.DumpsiteReport report, CancellationToken ct)
    {
        IQueryable<EcoMonitor.Domain.Entities.DumpsiteReport>? query = null;
        if (report.Source == ReportSource.Telegram && report.TelegramUserId.HasValue)
        {
            query = _db.DumpsiteReports.AsNoTracking()
                .Where(r => r.TelegramUserId == report.TelegramUserId);
        }
        else if (report.ReporterId.HasValue)
        {
            query = _db.DumpsiteReports.AsNoTracking()
                .Where(r => r.ReporterId == report.ReporterId);
        }

        if (query is null) return (0, 0, 0, 0);

        var byStatus = await query
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var pendingStatuses = new[]
        {
            DumpsiteStatus.New,
            DumpsiteStatus.InReview,
            DumpsiteStatus.Confirmed,
            DumpsiteStatus.CleanupInProgress,
            DumpsiteStatus.AwaitingVerification
        };

        var total    = byStatus.Sum(x => x.Count);
        var pending  = byStatus.Where(x => pendingStatuses.Contains(x.Status)).Sum(x => x.Count);
        var resolved = byStatus.Where(x => x.Status == DumpsiteStatus.Resolved).Sum(x => x.Count);
        var rejected = byStatus.Where(x => x.Status == DumpsiteStatus.Rejected).Sum(x => x.Count);
        return (total, pending, resolved, rejected);
    }
}
