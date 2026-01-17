using System.Security.Claims;
using VirtualRoulette.Applications.ActivityTracker;

namespace VirtualRoulette.Middleware;

public class ActivityTrackingMiddleware(
    RequestDelegate next,
    ILogger<ActivityTrackingMiddleware> logger
)
{

    public async Task InvokeAsync(HttpContext context, IUserActivityTracker activityTracker)
    {
        var isAutIsAuthenticated = context.User.Identity!.IsAuthenticated;
        if (isAutIsAuthenticated)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userId is not null)
            {
                var userIsActive = activityTracker.IsUserActive(userId);
                if (userIsActive)
                {
                    activityTracker.UpdateActivity(userId);
                    logger.LogDebug("Updated activity for user {UserId}", userId);

                }
                /*
                if (!userIsActive)
                {
                    // User's session has expired due to inactivity
                    logger.LogInformation("User {UserId} session expired due to inactivity", userId);
                    
                    // The cookie validation event will handle signing out the user
                    // Just continue processing the request
                }
                else
                {
                    // Update user activity timestamp
                    activityTracker.UpdateActivity(userId);
                    logger.LogDebug("Updated activity for user {UserId}", userId);
                }*/
            }
        }
        await next(context);
    }
}
