using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Common.Helpers;

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
            var userIdResult = UserHelper.GetUserId(context);
            
            if (userIdResult.IsSuccess)
            {
                var userIsActive = activityTracker.IsUserActive(userIdResult.Value);
                if (userIsActive)
                {
                    activityTracker.UpdateActivity(userIdResult.Value);
                    logger.LogDebug("Updated activity for user {UserId}", userIdResult);

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
