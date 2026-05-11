using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Queries.GetReportForCleanup;

public class GetReportForCleanupHandler : IRequestHandler<GetReportForCleanupQuery, CleanupReportDto?>
{
    private readonly IApplicationDbContext _db;

    public GetReportForCleanupHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CleanupReportDto?> Handle(GetReportForCleanupQuery request, CancellationToken ct)
    {
        var report = await _db.DumpsiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);

        if (report is null) return null;

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
            report.InspectorObservations,
            report.CleanupNotes,
            report.CleanupCrewId,
            report.CleanupStartedAt,
            report.CleanupCompletedAt,
            report.CreatedAt,
            report.UpdatedAt);
    }
}
