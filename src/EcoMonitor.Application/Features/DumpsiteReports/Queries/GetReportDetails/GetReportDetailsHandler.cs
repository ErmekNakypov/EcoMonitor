using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Queries.GetReportDetails;

public class GetReportDetailsHandler : IRequestHandler<GetReportDetailsQuery, ReportDetailsDto?>
{
    private readonly IApplicationDbContext _dbContext;

    public GetReportDetailsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return new ReportDetailsDto(
            report.Id,
            report.Description,
            report.Status,
            report.Latitude,
            report.Longitude,
            report.PhotoPaths,
            report.ResolutionNotes,
            report.ResolvedAt,
            report.CreatedAt,
            report.UpdatedAt);
    }
}
