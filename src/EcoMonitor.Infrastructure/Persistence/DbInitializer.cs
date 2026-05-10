using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoMonitor.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await dbContext.Database.MigrateAsync();

        foreach (var roleName in new[] { RoleNames.Administrator, RoleNames.Inspector, RoleNames.Citizen })
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
    }
}
