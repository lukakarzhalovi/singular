using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Shared.Helpers;

namespace VirtualRoulette.Presentation.Middleware;

public class ActivityTrackingMiddleware(
    RequestDelegate next,
    ILogger<ActivityTrackingMiddleware> logger
)
{
    public async Task InvokeAsync(HttpContext context, IUserActivityTracker activityTracker)
    {
        // Only track activity for authenticated users
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdResult = UserHelper.GetUserId(context);
            
            if (userIdResult.IsSuccess)
            {
                // Auto sign-out if user hasn't been active for 5 minutes
                if (!activityTracker.IsUserActive(userIdResult.Value))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    activityTracker.RemoveUser(userIdResult.Value);
                    
                    logger.LogInformation("User {UserId} automatically signed out due to inactivity", userIdResult.Value);
                    
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("User session expired due to inactivity");
                    return;
                }
                // Update last activity time on every request
                activityTracker.UpdateActivity(userIdResult.Value);
                logger.LogDebug("Updated activity for user {UserId}", userIdResult.Value);
            }
        }
        
        await next(context);
    }
}
