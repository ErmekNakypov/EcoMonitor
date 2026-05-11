using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
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
            report.CleanupNotes);
    }
}
