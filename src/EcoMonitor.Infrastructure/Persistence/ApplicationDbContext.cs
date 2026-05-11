using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence.Conversions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<WasteContainer> WasteContainers => Set<WasteContainer>();
    public DbSet<DumpsiteReport> DumpsiteReports => Set<DumpsiteReport>();
    public DbSet<AirQualityReading> AirQualityReadings => Set<AirQualityReading>();
    public DbSet<AirQualityStation> AirQualityStations => Set<AirQualityStation>();
    public DbSet<TelegramUserSession> TelegramUserSessions => Set<TelegramUserSession>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<IotDevice> IotDevices => Set<IotDevice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<ApplicationRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Force every DateTime/DateTime? property through UTC converters so
        // Npgsql's `timestamp with time zone` requirement is satisfied
        // regardless of where the value originated (form, JSON, code).
        var dateTimeConverter = new UtcDateTimeConverter();
        var nullableDateTimeConverter = new NullableUtcDateTimeConverter();

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        TouchUpdatedAt();
        return base.SaveChanges();
    }

    private void TouchUpdatedAt()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
