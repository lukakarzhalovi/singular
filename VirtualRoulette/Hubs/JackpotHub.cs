using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Applications.Jackpot;

namespace VirtualRoulette.Hubs;

[Authorize]
public class JackpotHub(
    IUserActivityTracker activityTracker,
    IJackpotService jackpotService,
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
            var currentJackpot = jackpotService.GetCurrentJackpot();
            await Clients.Caller.SendAsync("JackpotUpdated", currentJackpot);
            
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
        
        return jackpotService.GetCurrentJackpot();
    }
    
    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
