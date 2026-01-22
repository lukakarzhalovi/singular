using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Common.Helpers;
using VirtualRoulette.Persistence.InMemoryCache;

namespace VirtualRoulette.Hubs;

[Authorize]
public class JackpotHub(
    IJackpotInMemoryCache jackpotInMemoryCache,
    IJackpotHubConnectionTracker connectionTracker,
    ILogger<JackpotHub> logger)
    : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdResult = UserHelper.GetUserId(Context);
        
        if (userIdResult.IsSuccess)
        {
            var userId = userIdResult.Value;
            var connectionId = Context.ConnectionId;
            
            // Check if user already has a connection
            var oldConnectionId = connectionTracker.GetConnection(userId);
            
            if (oldConnectionId != null && oldConnectionId != connectionId)
            {
                // Remove old connection from group
                await Groups.RemoveFromGroupAsync(oldConnectionId, "JackpotSubscribers");
                
                // Send disconnect signal to old connection
                await Clients.Client(oldConnectionId).SendAsync("ForceDisconnect");
                
                logger.LogInformation(
                    "User {UserId} has existing connection {OldConnectionId}. Disconnecting old connection and replacing with {NewConnectionId}",
                    userId, oldConnectionId, connectionId);
            }
            
            // Register new connection
            connectionTracker.SetConnection(userId, connectionId);
            
            await Groups.AddToGroupAsync(connectionId, "JackpotSubscribers");
            
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("JackpotUpdated", currentJackpotResult.Value);
            }
            
            logger.LogInformation("User {UserId} connected to JackpotHub. ConnectionId: {ConnectionId}", 
                userId, connectionId);
        }
        
        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdResult = UserHelper.GetUserId(Context);
        
        if (userIdResult.IsSuccess)
        {
            var userId = userIdResult.Value;
            var connectionId = Context.ConnectionId;
            
            // Remove connection from tracker
            connectionTracker.RemoveConnection(userId);
            
            // Remove from group
            await Groups.RemoveFromGroupAsync(connectionId, "JackpotSubscribers");
            
            logger.LogInformation("User {UserId} disconnected from JackpotHub. ConnectionId: {ConnectionId}", 
                userId, connectionId);
            
            if (exception != null)
            {
                logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    public long GetCurrentJackpot()
    {
        var result = jackpotInMemoryCache.Get();
        return result.IsSuccess ? result.Value : 0;
    }
}
