using System.Globalization;
using System.Text;
using EcoMonitor.Application;
using EcoMonitor.Infrastructure;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
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

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("ru-RU");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
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
    .AddDataAnnotationsLocalization();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

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
