using EcoMonitor.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Queries.GetNextContainerCode;

public class GetNextContainerCodeHandler : IRequestHandler<GetNextContainerCodeQuery, string>
{
    private readonly IApplicationDbContext _dbContext;

    public GetNextContainerCodeHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> Handle(GetNextContainerCodeQuery request, CancellationToken cancellationToken)
    {
        var codes = await _dbContext.WasteContainers
            .AsNoTracking()
            .Where(c => c.Code.StartsWith("C-"))
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var code in codes)
        {
            if (code.Length > 2 && int.TryParse(code.AsSpan(2), out var n))
            {
                if (n > max) max = n;
            }
        }

        return $"C-{(max + 1):D5}";
    }
}
