using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Common;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Queries.GetReportForInspector;

public class GetReportForInspectorHandler : IRequestHandler<GetReportForInspectorQuery, InspectorReportDto?>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserLookupService _userLookup;

    public GetReportForInspectorHandler(IApplicationDbContext dbContext, IUserLookupService userLookup)
    {
        _dbContext = dbContext;
        _userLookup = userLookup;
    }

    public async Task<InspectorReportDto?> Handle(GetReportForInspectorQuery request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var ids = new List<Guid>();
        if (report.ReporterId.HasValue) ids.Add(report.ReporterId.Value);
        if (report.AssignedInspectorId.HasValue) ids.Add(report.AssignedInspectorId.Value);
        if (report.CleanupCrewId.HasValue) ids.Add(report.CleanupCrewId.Value);
        if (report.CleanupFlaggedByCrewId.HasValue) ids.Add(report.CleanupFlaggedByCrewId.Value);

        var district = report.DistrictId.HasValue
            ? await _dbContext.Districts.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == report.DistrictId.Value, cancellationToken)
            : null;
        if (district?.AssignedInspectorId is { } districtInspectorId
            && !ids.Contains(districtInspectorId))
        {
            ids.Add(districtInspectorId);
        }

        IReadOnlyDictionary<Guid, UserSummaryDto> users = ids.Count > 0
            ? await _userLookup.GetByIdsAsync(ids, cancellationToken)
            : new Dictionary<Guid, UserSummaryDto>();

        var reporter = report.ReporterId.HasValue ? users.GetValueOrDefault(report.ReporterId.Value) : null;
        var inspector = report.AssignedInspectorId.HasValue
            ? users.GetValueOrDefault(report.AssignedInspectorId.Value)
            : null;
        var crew = report.CleanupCrewId.HasValue
            ? users.GetValueOrDefault(report.CleanupCrewId.Value)
            : null;
        var flagger = report.CleanupFlaggedByCrewId.HasValue
            ? users.GetValueOrDefault(report.CleanupFlaggedByCrewId.Value)
            : null;

        var inspectionPhotos = await _dbContext.DumpsiteInspectionPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

        var beforePhotos = await _dbContext.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.BeforeCleanup)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

        var afterPhotos = await _dbContext.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.AfterCleanup)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

        // Reporter track record. Match by ReporterId for web, TelegramUserId
        // for bot, otherwise leave zeros.
        var stats = await GetReporterStatsAsync(report, cancellationToken);

        var events = await _dbContext.DumpsiteReportEvents
            .AsNoTracking()
            .Where(e => e.ReportId == report.Id)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new ReportEventDto(e.EventType, e.OccurredAt, e.ActorRole, e.ActorDisplayName, e.Notes))
            .ToListAsync(cancellationToken);

        var appealPhotos = await _dbContext.DumpsiteAppealPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

        var flagEvidencePhotos = await _dbContext.DumpsiteCleanupPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id && p.Type == CleanupPhotoType.FlagEvidence)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

        return new InspectorReportDto(
            report.Id,
            report.Description,
            report.Status,
            report.Latitude,
            report.Longitude,
            report.PhotoPaths,
            report.ReporterId,
            reporter?.Email,
            reporter?.FullName,
            report.AssignedInspectorId,
            inspector?.Email,
            report.ResolutionNotes,
            report.ResolvedAt,
            report.CreatedAt,
            report.UpdatedAt,
            report.Source,
            report.TelegramUserName,
            report.InspectorObservations,
            inspectionPhotos,
            beforePhotos,
            afterPhotos,
            report.CleanupCrewId,
            crew?.FullName,
            report.CleanupStartedAt,
            report.CleanupCompletedAt,
            report.CleanupNotes,
            report.AutoTriageReason,
            report.TelegramUserId,
            stats.Total,
            stats.Pending,
            stats.Resolved,
            stats.Rejected,
            report.AppealedAt,
            report.AppealReason,
            appealPhotos,
            report.AppealReviewedAt,
            report.AppealResolutionNotes,
            report.AppealOutcome,
            report.ClosedAt,
            events,
            report.CleanupRejectionReason,
            report.CleanupRejectionNotes,
            report.CleanupFlaggedAt,
            flagger?.FullName,
            report.ReassignCount,
            flagEvidencePhotos,
            report.DistrictId,
            district?.NameRu,
            district?.NameEn,
            district?.ColorHex,
            district?.AssignedInspectorId.HasValue == true
                ? users.GetValueOrDefault(district.AssignedInspectorId.Value)?.FullName
                : null);
    }

    private async Task<(int Total, int Pending, int Resolved, int Rejected)> GetReporterStatsAsync(
        EcoMonitor.Domain.Entities.DumpsiteReport report, CancellationToken ct)
    {
        IQueryable<EcoMonitor.Domain.Entities.DumpsiteReport>? query = null;
        if (report.Source == ReportSource.Telegram && report.TelegramUserId.HasValue)
        {
            query = _dbContext.DumpsiteReports.AsNoTracking()
                .Where(r => r.TelegramUserId == report.TelegramUserId);
        }
        else if (report.ReporterId.HasValue)
        {
            query = _dbContext.DumpsiteReports.AsNoTracking()
                .Where(r => r.ReporterId == report.ReporterId);
        }

        if (query is null) return (0, 0, 0, 0);

        // Single SQL roundtrip: status histogram for this reporter.
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
