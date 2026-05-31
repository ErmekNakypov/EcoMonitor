using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    DbSet<ContainerFillReading> ContainerFillReadings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Lets callers wrap multi-statement work (e.g. ExecuteDelete + Add + SaveChanges)
    // in a single explicit transaction without casting to ApplicationDbContext.
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
