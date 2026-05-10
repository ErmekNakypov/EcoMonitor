using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Infrastructure.AirQuality;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Cookie.Name = "EcoMonitor.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        services.AddMemoryCache();

        services.AddHttpClient("openaq", client =>
        {
            client.BaseAddress = new Uri("https://api.openaq.org/");
            client.DefaultRequestHeaders.Add("User-Agent", "EcoMonitor/1.0");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IAirQualityProvider, OpenAqAirQualityProvider>();
        services.AddScoped<IAirQualityRepository, AirQualityRepository>();
        services.AddScoped<IAirQualityIngestionRunner, AirQualityIngestionRunner>();

        services.AddHostedService<AirQualityIngestionService>();

        return services;
    }
}
