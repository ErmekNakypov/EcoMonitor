using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Infrastructure.Identity;

public class UserLookupService : IUserLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public UserLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserSummaryDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSummaryDto(u.Id, u.Email!, u.FullName))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummaryDto>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserSummaryDto>();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserSummaryDto(u.Id, u.Email!, u.FullName))
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Id);
    }
}
