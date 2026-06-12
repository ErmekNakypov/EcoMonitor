using System.Globalization;
using System.Text;
using EcoMonitor.Application;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Common;
using EcoMonitor.Infrastructure;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Web;
using EcoMonitor.Web.Helpers;
using EcoMonitor.Web.Hubs;
using EcoMonitor.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ecomonitor-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/ecomonitor-.log", rollingInterval: RollingInterval.Day));

    var supportedCultures = new[]
    {
        new CultureInfo("ru-RU"),
        new CultureInfo("en-US"),
        new CultureInfo("ky-KG")
    };

    // Sets up IStringLocalizer / IViewLocalizer to load .resx files from the
    // "Resources" folder under the web project root. Required for the rest of
    // the localization pipeline (AddViewLocalization + .resx) to work.
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("ru-RU");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;

        // Resolution order: cookie (set by /Localization/SetCulture or on
        // login from ApplicationUser.PreferredLanguage), then Accept-Language
        // from the browser. Query-string and form providers (the framework
        // defaults) are intentionally dropped — culture should be a sticky
        // user preference, not something a link can change for one request.
        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        };
    });

    builder.Services.AddControllersWithViews(options =>
    {
        options.ModelBinderProviders.Insert(0, new InvariantDoubleModelBinderProvider());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // Route every view-model's [Display(Name=…)] and [Required] /
        // [StringLength] / [Compare] / etc. ErrorMessage lookups to a
        // resource bucket dedicated to that view-model. With
        // ResourcesPath="Resources" the framework resolves
        // EcoMonitor.Web.Models.Account.LoginViewModel to
        // Resources/Models/Account/LoginViewModel.{culture}.resx.
        // Per-VM scoping (over a single shared bucket) keeps form keys
        // co-located with the form and avoids cross-form key collisions
        // when two unrelated fields both happen to be called "Email".
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(type);
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<ISensorRealtimePublisher, SignalRSensorRealtimePublisher>();

    // JWT bearer is added as a *named* scheme alongside the existing Identity cookie
    // scheme. The default authenticate/challenge scheme stays the cookie scheme set
    // up by AddIdentity inside AddInfrastructure. Browser users continue to log in
    // via cookies; only endpoints with [Authorize(Policy = "DeviceOnly")] use JWT.
    var jwtSecret = builder.Configuration["Jwt:SecretKey"]
        ?? "dev-fallback-secret-do-not-use-in-prod-this-is-only-for-development-32-chars-min";

    builder.Services.AddAuthentication()
        .AddJwtBearer("DeviceJwt", options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("DeviceOnly", policy =>
        {
            policy.AuthenticationSchemes = new List<string> { "DeviceJwt" };
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("type", "device");
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.Logger.LogWarning(
            "DEV mode: weak password policy enabled. Set ASPNETCORE_ENVIRONMENT=Production to enforce strong passwords.");
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseRouting();

    app.UseRequestLocalization();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapHub<SensorHub>("/hubs/sensors");

    // Wire the Domain-layer enum-display resolver to a request-culture-aware
    // IStringLocalizer over Resources/EnumDisplayNames.{culture}.resx. Keys
    // are "{EnumType}.{Value}" (e.g. "DumpsiteStatus.New"). If a key is
    // missing the helper returns null and GetDisplayName() falls back to
    // the English [Display(Name)] attribute.
    var enumLocalizerFactory = app.Services.GetRequiredService<IStringLocalizerFactory>();
    var enumLocalizer = enumLocalizerFactory.Create(
        baseName: "EnumDisplayNames",
        location: typeof(Program).Assembly.GetName().Name!);
    EnumDisplayLocalization.Resolver = enumValue =>
    {
        var key = $"{enumValue.GetType().Name}.{enumValue}";
        var localized = enumLocalizer[key];
        return localized.ResourceNotFound ? null : localized.Value;
    };

    // AqiHelper.GetLabel + DateHelpers.FormatRelative + DateHelpers.FormatDate
    // are static helpers consumed both from Razor views and from API JSON
    // payloads (Controllers/Api/MapDataController.cs). Wire each to the
    // SharedResource bundle via the same hook pattern as the enum resolver
    // so the JSON values follow the request culture too.
    var sharedLocalizer = enumLocalizerFactory.Create(typeof(SharedResource));

    AqiHelper.LabelResolver = level =>
    {
        var key = $"Aqi.Label.{level}";
        var s = sharedLocalizer[key];
        return s.ResourceNotFound ? null : s.Value;
    };

    DateHelpers.RelativeResolver = (unit, count) =>
    {
        var key = unit switch
        {
            RelativeTimeUnit.JustNow      => "Time.JustNow",
            RelativeTimeUnit.MinuteAgo    => "Time.MinuteAgo",
            RelativeTimeUnit.MinutesAgo   => "Time.MinutesAgoFormat",
            RelativeTimeUnit.HourAgo      => "Time.HourAgo",
            RelativeTimeUnit.HoursAgo     => "Time.HoursAgoFormat",
            RelativeTimeUnit.Yesterday    => "Time.Yesterday",
            RelativeTimeUnit.DayAgo       => "Time.DayAgo",
            RelativeTimeUnit.DaysAgo      => "Time.DaysAgoFormat",
            RelativeTimeUnit.WeekAgo      => "Time.WeekAgo",
            RelativeTimeUnit.WeeksAgo     => "Time.WeeksAgoFormat",
            RelativeTimeUnit.MonthAgo     => "Time.MonthAgo",
            RelativeTimeUnit.MonthsAgo    => "Time.MonthsAgoFormat",
            RelativeTimeUnit.YearAgo      => "Time.YearAgo",
            RelativeTimeUnit.YearsAgo     => "Time.YearsAgoFormat",
            _ => null
        };
        if (key is null) return null;
        var s = sharedLocalizer[key];
        if (s.ResourceNotFound) return null;
        return s.Value.Contains("{0}") ? string.Format(s.Value, count) : s.Value;
    };

    DateHelpers.CalendarMarkerResolver = marker =>
    {
        var key = marker switch
        {
            CalendarDayMarker.Today     => "Time.Today",
            CalendarDayMarker.Yesterday => "Time.Yesterday",
            CalendarDayMarker.Tomorrow  => "Time.Tomorrow",
            _ => null
        };
        if (key is null) return null;
        var s = sharedLocalizer[key];
        return s.ResourceNotFound ? null : s.Value;
    };

    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
