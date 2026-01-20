using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Persistence.InMemoryCache;

namespace VirtualRoulette.Hubs;

[Authorize]
public class JackpotHub(
    IUserActivityTracker activityTracker,
    IJackpotInMemoryCache jackpotInMemoryCache,
    ILogger<JackpotHub> logger)
    : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        
        if (userId != null)
        {
            // Track user activity
            activityTracker.UpdateActivity(userId);
            
            // Add to jackpot subscribers group
            await Groups.AddToGroupAsync(Context.ConnectionId, "JackpotSubscribers");
            
            // Send current jackpot to the newly connected client
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("JackpotUpdated", currentJackpotResult.Value);
            }
            
            logger.LogInformation("User {UserId} connected to JackpotHub. ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "JackpotSubscribers");
            
            logger.LogInformation("User {UserId} disconnected from JackpotHub. ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
            
            if (exception != null)
            {
                logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    
    public void Heartbeat()
    {
        var userId = GetUserId();
        
        if (userId != null)
        {
            activityTracker.UpdateActivity(userId);
            logger.LogDebug("Heartbeat received from user {UserId}", userId);
        }
    }
    
    public long GetCurrentJackpot()
    {
        var userId = GetUserId();
        
        if (userId != null)
        {
            activityTracker.UpdateActivity(userId);
        }
        
        var result = jackpotInMemoryCache.Get();
        return result.IsSuccess ? result.Value : 0;
    }
    
    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
