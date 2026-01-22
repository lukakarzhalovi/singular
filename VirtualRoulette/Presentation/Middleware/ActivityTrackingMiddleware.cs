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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdResult = UserHelper.GetUserId(context);
            
            if (userIdResult.IsSuccess)
            {

                if (!activityTracker.IsUserActive(userIdResult.Value))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    activityTracker.RemoveUser(userIdResult.Value);
                    
                    logger.LogInformation("User {UserId} automatically signed out due to inactivity", userIdResult.Value);
                    
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("User session expired due to inactivity");
                    return;
                }
                activityTracker.UpdateActivity(userIdResult.Value);
                logger.LogDebug("Updated activity for user {UserId}", userIdResult.Value);
            }
        }
        
        await next(context);
    }
}
