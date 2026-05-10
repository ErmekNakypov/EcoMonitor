using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<DumpsiteReport> DumpsiteReports { get; }
    DbSet<WasteContainer> WasteContainers { get; }
    DbSet<AirQualityReading> AirQualityReadings { get; }
    DbSet<AirQualityStation> AirQualityStations { get; }
    DbSet<TelegramUserSession> TelegramUserSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
