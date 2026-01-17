using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Applications.Jackpot;
using VirtualRoulette.Applications.PasswordHasher;
using VirtualRoulette.Persistence;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Configuration;

public static class ServiceCollectionExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherServiceService>();
        services.AddSingleton<IUserActivityTracker, UserActivityTracker>();
        services.AddSingleton<IJackpotService, JackpotService>(); 
        services.AddScoped<AuthorizationServiceValidator>();
    }
    
    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
    }
    
    public static void AddAuthentification(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/api/v1/Authorize/signin";
        
                // Set cookie expiration to 5 minutes with sliding expiration
                // This means the cookie will expire 5 minutes after the last request
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.SlidingExpiration = true;
        
                // Custom validation to check server-side activity tracking
                options.Events.OnValidatePrincipal = async context =>
                {
                    var activityTracker = context.HttpContext.RequestServices.GetRequiredService<IUserActivityTracker>();
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
                    if (userId != null && !activityTracker.IsUserActive(userId))
                    {
                        // User has been inactive for more than 5 minutes
                        // Reject the cookie and sign out the user
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("User {UserId} automatically signed out due to inactivity", userId);
                    }
                };
            });
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(5);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
    }
    
    public static void AddMiddleware(this IServiceCollection services)
    {
        //Todo add middleware if i need
    }
    
    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);

        services.AddMemoryCache();
        services.AddSignalR();
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("VirtualRouletteDb"));
    }
}