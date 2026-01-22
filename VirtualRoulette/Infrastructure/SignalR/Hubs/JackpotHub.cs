using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VirtualRoulette.Infrastructure.Persistence.Caching;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Helpers;

namespace VirtualRoulette.Infrastructure.SignalR.Hubs;

[Authorize]
public class JackpotHub(
    IJackpotInMemoryCache jackpotInMemoryCache,
    IJackpotHubConnectionTracker connectionTracker,
    ILogger<JackpotHub> logger,
    IOptions<SignalRSettings> signalRSettings)
    : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdResult = UserHelper.GetUserId(Context);
        var settings = signalRSettings.Value;
        
        // Only proceed if user is authenticated
        if (userIdResult.IsSuccess)
        {
            var userId = userIdResult.Value;
            var connectionId = Context.ConnectionId;
            
            // If user already has a connection, disconnect the old one
            var oldConnectionId = connectionTracker.GetConnection(userId);
            
            if (oldConnectionId != null && oldConnectionId != connectionId)
            {
                await Groups.RemoveFromGroupAsync(oldConnectionId, settings.JackpotGroupName);
                await Clients.Client(oldConnectionId).SendAsync(settings.ForceDisconnectMethod);
                
                logger.LogInformation(
                    "User {UserId} has existing connection {OldConnectionId}. Disconnecting old connection and replacing with {NewConnectionId}",
                    userId, oldConnectionId, connectionId);
            }
            
            // Add user to jackpot group to receive updates
            connectionTracker.SetConnection(userId, connectionId);
            await Groups.AddToGroupAsync(connectionId, settings.JackpotGroupName);
            
            // Send current jackpot value to newly connected client
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsSuccess)
            {
                await Clients.Caller.SendAsync(settings.JackpotUpdatedMethod, currentJackpotResult.Value);
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
            await Groups.RemoveFromGroupAsync(connectionId, signalRSettings.Value.JackpotGroupName);
            
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
