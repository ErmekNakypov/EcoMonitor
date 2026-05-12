using EcoMonitor.Domain.Entities;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IDistrictResolver
{
    Task<District?> ResolveAsync(double latitude, double longitude, CancellationToken ct = default);
    Task<IReadOnlyList<District>> GetAllAsync(CancellationToken ct = default);

    // Clear the in-memory cache (used after seeding / admin reassignment).
    void InvalidateCache();
}
