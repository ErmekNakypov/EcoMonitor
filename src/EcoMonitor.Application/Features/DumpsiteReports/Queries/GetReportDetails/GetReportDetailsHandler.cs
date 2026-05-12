using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Common;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetReportDetails;

public class GetReportDetailsHandler : IRequestHandler<GetReportDetailsQuery, ReportDetailsDto?>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserLookupService _userLookup;

    public GetReportDetailsHandler(IApplicationDbContext dbContext, IUserLookupService userLookup)
    {
        _dbContext = dbContext;
        _userLookup = userLookup;
    }

    public async Task<ReportDetailsDto?> Handle(GetReportDetailsQuery request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null || report.ReporterId != request.RequestingUserId)
        {
            return null;
        }

        var appealPhotos = await _dbContext.DumpsiteAppealPhotos
            .AsNoTracking()
            .Where(p => p.ReportId == report.Id)
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.FilePath)
            .ToListAsync(cancellationToken);

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

        var events = await _dbContext.DumpsiteReportEvents
            .AsNoTracking()
            .Where(e => e.ReportId == report.Id)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new ReportEventDto(e.EventType, e.OccurredAt, e.ActorRole, e.ActorDisplayName, e.Notes))
            .ToListAsync(cancellationToken);

        var ids = new List<Guid>();
        if (report.CleanupCrewId.HasValue) ids.Add(report.CleanupCrewId.Value);
        if (report.VerifiedByInspectorId.HasValue) ids.Add(report.VerifiedByInspectorId.Value);
        IReadOnlyDictionary<Guid, UserSummaryDto> users = ids.Count > 0
            ? await _userLookup.GetByIdsAsync(ids, cancellationToken)
            : new Dictionary<Guid, UserSummaryDto>();

        var crewName = report.CleanupCrewId.HasValue
            ? users.GetValueOrDefault(report.CleanupCrewId.Value)?.FullName
            : null;
        var verifierName = report.VerifiedByInspectorId.HasValue
            ? users.GetValueOrDefault(report.VerifiedByInspectorId.Value)?.FullName
            : null;

        return new ReportDetailsDto(
            report.Id,
            report.Description,
            report.Status,
            report.Latitude,
            report.Longitude,
            report.PhotoPaths,
            inspectionPhotos,
            beforePhotos,
            afterPhotos,
            report.ResolutionNotes,
            report.ResolvedAt,
            report.CreatedAt,
            report.UpdatedAt,
            report.AppealedAt,
            report.AppealReason,
            appealPhotos,
            report.AppealReviewedAt,
            report.AppealResolutionNotes,
            report.AppealOutcome,
            report.ClosedAt,
            crewName,
            report.CleanupCompletedAt,
            verifierName,
            events);
    }
}
