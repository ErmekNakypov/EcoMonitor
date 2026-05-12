using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.Notifications;
using EcoMonitor.Infrastructure.AirQuality;
using EcoMonitor.Infrastructure.Auth;
using EcoMonitor.Infrastructure.Containers;
using EcoMonitor.Infrastructure.Email;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Infrastructure.Storage;
using EcoMonitor.Infrastructure.Telegram;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
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
            if (environment.IsDevelopment())
            {
                // Loose policy for fast local development. Production gets the
                // strict policy in the else branch below.
                options.Password.RequiredLength = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
            }
            else
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 4;
            }

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

        services.AddHttpClient("iqair", client =>
        {
            client.BaseAddress = new Uri("https://api.airvisual.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "EcoMonitor/1.0");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient("overpass", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EcoMonitor/1.0 (academic project)");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IAirQualityProvider, OpenAqAirQualityProvider>();
        services.AddScoped<IAirQualityProvider, IqAirAirQualityProvider>();
        services.AddScoped<IAirQualityRepository, AirQualityRepository>();
        services.AddScoped<IAirQualityIngestionRunner, AirQualityIngestionRunner>();
        services.AddScoped<IContainerImportService, OsmContainerImporter>();

        services.AddSingleton<BotLocalizer>();
        services.AddScoped<ITelegramDialogService, TelegramDialogService>();
        services.AddScoped<ITelegramNotificationService, TelegramReportNotificationService>();

        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailQueue, DbEmailQueue>();
        services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
        services.AddScoped<IReportNotificationService, EmailReportNotificationService>();
        services.AddScoped<IRoleNotificationService, RoleNotificationService>();
        services.AddScoped<IAutoTriageService, EcoMonitor.Infrastructure.Triage.BishkekAutoTriageService>();
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.Section));

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHostedService<AirQualityIngestionService>();
        services.AddHostedService<TelegramBotHostedService>();
        services.AddHostedService<EmailSenderHostedService>();
        services.AddHostedService<EcoMonitor.Infrastructure.BackgroundServices.AutoCloseExpiredReportsService>();

        return services;
    }
}
