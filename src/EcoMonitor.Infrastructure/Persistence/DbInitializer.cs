using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Persistence;

public static class DbInitializer
{
    private sealed record DevAccount(string Email, string FullName, string Role, string? DistrictCode = null);

    private static readonly DevAccount[] DevAccounts =
    {
        new("nakypoverm+admin@kstu.kg",             "Admin Manager",                       RoleNames.Administrator),
        new("nakypoverm+inspector1@kstu.kg",        "Inspector One",                       RoleNames.Inspector),
        new("nakypoverm+inspector2@kstu.kg",        "Inspector Two",                       RoleNames.Inspector),
        new("nakypoverm+inspector_sverdlov@kstu.kg","Инспектор Свердловского района",      RoleNames.Inspector, "SVERDLOV"),
        new("nakypoverm+inspector_pervomay@kstu.kg","Инспектор Первомайского района",      RoleNames.Inspector, "PERVOMAY"),
        new("nakypoverm+inspector_lenin@kstu.kg",   "Инспектор Ленинского района",         RoleNames.Inspector, "LENIN"),
        new("nakypoverm+inspector_oktyabr@kstu.kg", "Инспектор Октябрьского района",       RoleNames.Inspector, "OKTYABR"),
        new("nakypoverm+crew1@kstu.kg",             "Tazalyk Brigade One",                 RoleNames.CleanupCrew),
        new("nakypoverm+crew2@kstu.kg",             "Tazalyk Brigade Two",                 RoleNames.CleanupCrew),
        new("nakypoverm+citizen@kstu.kg",           "Test Citizen",                        RoleNames.Citizen)
    };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();

        await dbContext.Database.MigrateAsync();

        foreach (var roleName in new[] { RoleNames.Administrator, RoleNames.Inspector, RoleNames.Citizen, RoleNames.CleanupCrew })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        var adminEmail = configuration["DefaultAdmin:Email"];
        var adminPassword = configuration["DefaultAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    IsActive = true,
                    PreferredLanguage = "ru"
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, RoleNames.Administrator);
                }
            }
        }

        // Districts are city reference data — seed in every environment.
        await BishkekDistrictsSeeder.SeedAsync(dbContext, logger);

        if (environment.IsDevelopment())
        {
            await SeedDevTestAccountsAsync(userManager, logger);
            await LinkDistrictInspectorsAsync(dbContext, userManager, logger);
        }
    }

    // Seeds the role-targeted test accounts using the user's KSTU mailbox
    // via Gmail-style plus-modifier aliases. Idempotent per-account: each
    // missing email is created independently, so adding new specs later
    // (e.g. district inspectors) backfills correctly even when older accounts
    // already exist.
    private static async Task SeedDevTestAccountsAsync(
        UserManager<ApplicationUser> userManager,
        ILogger? logger)
    {
        foreach (var spec in DevAccounts)
        {
            var existing = await userManager.FindByEmailAsync(spec.Email);
            if (existing is not null) continue;

            var user = new ApplicationUser
            {
                UserName = spec.Email,
                Email = spec.Email,
                EmailConfirmed = true,
                FullName = spec.FullName,
                IsActive = true,
                PreferredLanguage = "ru"
            };

            var result = await userManager.CreateAsync(user, "123");
            if (!result.Succeeded)
            {
                logger?.LogWarning(
                    "Dev seed: could not create {Email}: {Errors}",
                    spec.Email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, spec.Role);
            logger?.LogInformation("Dev seed: created {Email} ({Role})", spec.Email, spec.Role);
        }
    }

    // After both districts and inspector accounts exist, set each district's
    // AssignedInspectorId so InReview reports route correctly. Idempotent:
    // skips districts that already have an inspector assigned.
    private static async Task LinkDistrictInspectorsAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger? logger)
    {
        var districtAccounts = DevAccounts.Where(a => a.DistrictCode is not null).ToList();
        if (districtAccounts.Count == 0) return;

        var dirty = false;
        foreach (var spec in districtAccounts)
        {
            var district = await db.Districts.FirstOrDefaultAsync(d => d.Code == spec.DistrictCode);
            if (district is null)
            {
                logger?.LogWarning("Dev seed: district {Code} not found for {Email}", spec.DistrictCode, spec.Email);
                continue;
            }
            if (district.AssignedInspectorId is not null) continue;

            var user = await userManager.FindByEmailAsync(spec.Email);
            if (user is null) continue;

            district.AssignedInspectorId = user.Id;
            district.UpdatedAt = DateTime.UtcNow;
            dirty = true;
            logger?.LogInformation("Dev seed: linked district {Code} -> {Email}", spec.DistrictCode, spec.Email);
        }
        if (dirty) await db.SaveChangesAsync();
    }
}
