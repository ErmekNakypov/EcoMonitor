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
    DbSet<EmailMessage> EmailMessages { get; }
    DbSet<IotDevice> IotDevices { get; }
    DbSet<DumpsiteInspectionPhoto> DumpsiteInspectionPhotos { get; }
    DbSet<DumpsiteCleanupPhoto> DumpsiteCleanupPhotos { get; }
    DbSet<DumpsiteAppealPhoto> DumpsiteAppealPhotos { get; }
    DbSet<DumpsiteReportEvent> DumpsiteReportEvents { get; }
    DbSet<District> Districts { get; }
    DbSet<DistrictBoundaryPoint> DistrictBoundaryPoints { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
