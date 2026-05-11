using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Persistence;

public static class DbInitializer
{
    private const string DevSeedEmailPrefix = "nakypoverm+";

    private sealed record DevAccount(string Email, string FullName, string Role);

    private static readonly DevAccount[] DevAccounts =
    {
        new("nakypoverm+admin@kstu.kg",      "Admin Manager",         RoleNames.Administrator),
        new("nakypoverm+inspector1@kstu.kg", "Inspector One",         RoleNames.Inspector),
        new("nakypoverm+inspector2@kstu.kg", "Inspector Two",         RoleNames.Inspector),
        new("nakypoverm+crew1@kstu.kg",      "Tazalyk Brigade One",   RoleNames.CleanupCrew),
        new("nakypoverm+crew2@kstu.kg",      "Tazalyk Brigade Two",   RoleNames.CleanupCrew),
        new("nakypoverm+citizen@kstu.kg",    "Test Citizen",          RoleNames.Citizen)
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

        if (environment.IsDevelopment())
        {
            await SeedDevTestAccountsAsync(userManager, logger);
        }
    }

    // Seeds the six role-targeted test accounts using the user's KSTU mailbox
    // via Gmail-style plus-modifier aliases. Idempotent: if ANY user with the
    // `nakypoverm+` prefix already exists, the seeder is a no-op.
    private static async Task SeedDevTestAccountsAsync(
        UserManager<ApplicationUser> userManager,
        ILogger? logger)
    {
        var anyExisting = await userManager.Users
            .AnyAsync(u => u.Email != null && u.Email.StartsWith(DevSeedEmailPrefix));
        if (anyExisting)
        {
            return;
        }

        foreach (var spec in DevAccounts)
        {
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
}
