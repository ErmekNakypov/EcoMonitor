using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetContainersList;

public class GetContainersListHandler : IRequestHandler<GetContainersListQuery, ContainersListResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetContainersListHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContainersListResult> Handle(GetContainersListQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _dbContext.WasteContainers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(s) || c.Address.ToLower().Contains(s));
        }

        if (request.TypeFilter.HasValue)
        {
            query = query.Where(c => c.Type == request.TypeFilter.Value);
        }

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(c => c.Status == request.StatusFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContainerListItemDto(
                c.Id,
                c.Code,
                c.Address,
                c.Latitude,
                c.Longitude,
                c.Type,
                c.Status,
                c.Capacity,
                c.InstalledAt,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ContainersListResult(rows, totalCount, page, pageSize, totalPages);
    }
}
