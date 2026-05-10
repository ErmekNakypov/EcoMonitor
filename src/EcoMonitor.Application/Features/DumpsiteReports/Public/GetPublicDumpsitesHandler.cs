using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.DumpsiteReports.Public;

public class GetPublicDumpsitesHandler : IRequestHandler<GetPublicDumpsitesQuery, IReadOnlyList<PublicDumpsiteDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPublicDumpsitesHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicDumpsiteDto>> Handle(GetPublicDumpsitesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.Status == DumpsiteStatus.Confirmed || r.Status == DumpsiteStatus.Resolved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new
            {
                r.Id,
                r.Description,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.CreatedAt,
                r.ResolvedAt,
                r.PhotoPaths
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PublicDumpsiteDto(
            r.Id,
            r.Description.Length > 100 ? r.Description.Substring(0, 100) + "…" : r.Description,
            r.Status,
            r.Latitude,
            r.Longitude,
            r.CreatedAt,
            r.ResolvedAt,
            r.PhotoPaths.Count)).ToList();
    }
}
