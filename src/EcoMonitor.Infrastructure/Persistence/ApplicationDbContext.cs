using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Infrastructure.Identity;
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
