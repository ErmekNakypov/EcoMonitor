using EcoMonitor.Application.Common.Models;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IUserLookupService
{
    Task<UserSummaryDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, UserSummaryDto>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
