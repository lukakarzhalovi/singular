using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Core.Services.Authorization;
using VirtualRoulette.Core.Services.Bet;
using VirtualRoulette.Core.Services.PasswordHasher;
using VirtualRoulette.Core.Services.User;
using VirtualRoulette.Configuration.Settings;
using VirtualRoulette.Infrastructure.SignalR;
using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Infrastructure.Persistence;
using VirtualRoulette.Infrastructure.Persistence.Caching;
using VirtualRoulette.Infrastructure.Persistence.Repositories;

namespace VirtualRoulette.Configuration;

public static class ServiceCollectionExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IUserActivityTracker, UserActivityTracker>();
        services.AddSingleton<IJackpotHubConnectionTracker, JackpotHubConnectionTracker>();
        services.AddSingleton<IJackpotInMemoryCache, JackpotInMemoryCache>();
        services.AddScoped<AuthorizationServiceValidator>();
        services.AddScoped<IRouletteService, RouletteService>();
        services.AddScoped<IUserService, UserService>();
    }
    
    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBetRepository, BetRepository>();
    }
    
    public static void AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingSettings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() 
                                   ?? throw new InvalidOperationException("Rate limiting settings are required");
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(rateLimitingSettings.PolicyName, limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitingSettings.PermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitingSettings.WindowSeconds);
                limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitingSettings.QueueLimit;
            });
        });
    }
        public static void AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() 
                           ?? throw new InvalidOperationException("CORS settings are required");
        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
    
    public static void AddAuthentification(this IServiceCollection services, IConfiguration configuration)
    {
        var authSettings = configuration.GetSection("Authentication").Get<AuthenticationSettings>() 
            ?? throw new InvalidOperationException("Authentication settings are required");

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = authSettings.Cookie.HttpOnly;
                options.Cookie.SecurePolicy = authSettings.Cookie.SecurePolicy switch
                {
                    "Always" => CookieSecurePolicy.Always,
                    "SameAsRequest" => CookieSecurePolicy.SameAsRequest,
                    "None" => CookieSecurePolicy.None,
                    _ => CookieSecurePolicy.Always
                };
                options.Cookie.SameSite = authSettings.Cookie.SameSite switch
                {
                    "None" => SameSiteMode.None,
                    "Lax" => SameSiteMode.Lax,
                    "Strict" => SameSiteMode.Strict,
                    _ => SameSiteMode.None
                };
                options.LoginPath = authSettings.LoginPath;
        
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
        
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };

                options.ExpireTimeSpan = TimeSpan.FromMinutes(authSettings.ExpirationMinutes);
                options.SlidingExpiration = authSettings.SlidingExpiration;
                
            });
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(authSettings.SessionIdleTimeoutMinutes);
            options.Cookie.HttpOnly = authSettings.Cookie.HttpOnly;
            options.Cookie.IsEssential = authSettings.Cookie.IsEssential;
        });
    }

    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsSettings>(configuration.GetSection("Cors"));
        services.Configure<RateLimitingSettings>(configuration.GetSection("RateLimiting"));
        services.Configure<AuthenticationSettings>(configuration.GetSection("Authentication"));
        services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
        services.Configure<SignalRSettings>(configuration.GetSection("SignalR"));
        services.Configure<ActivityTrackingSettings>(configuration.GetSection("ActivityTracking"));
        services.Configure<JackpotSettings>(configuration.GetSection("Jackpot"));
        services.Configure<ApiSettings>(configuration.GetSection("Api"));
        services.Configure<FilterSettings>(configuration.GetSection("Filters"));

        AddDatabase(services, configuration);

        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSignalR();
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var databaseSettings = configuration.GetSection("Database").Get<DatabaseSettings>() 
            ?? throw new InvalidOperationException("Database settings are required");
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseSettings.DatabaseName));
    }
}