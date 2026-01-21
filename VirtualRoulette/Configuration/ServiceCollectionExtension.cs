using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Applications.Bet;
using VirtualRoulette.Applications.PasswordHasher;
using VirtualRoulette.Applications.User;
using VirtualRoulette.Persistence;
using VirtualRoulette.Persistence.InMemoryCache;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Configuration;

public static class ServiceCollectionExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherServiceService>();
        services.AddSingleton<IUserActivityTracker, UserActivityTracker>();
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
    
    public static void AddAuthentification(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.LoginPath = "/api/v1/Authorize/signin";
        
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

                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.SlidingExpiration = true;
                
            });
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(5);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
    }

    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);

        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSignalR();
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        //todo add configuration
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("VirtualRouletteDb"));
    }
}