using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Common.Helpers;
using VirtualRoulette.Persistence.InMemoryCache;

namespace VirtualRoulette.Hubs;

[Authorize]
public class JackpotHub(
    IJackpotInMemoryCache jackpotInMemoryCache,
    ILogger<JackpotHub> logger)
    : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdResult = UserHelper.GetUserId(Context);
        
        if (userIdResult.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "JackpotSubscribers");
            
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("JackpotUpdated", currentJackpotResult.Value);
            }
            
            logger.LogInformation("User {UserId} connected to JackpotHub. ConnectionId: {ConnectionId}", 
                userIdResult.Value, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdResult = UserHelper.GetUserId(Context);
        
        if (userIdResult.IsSuccess)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "JackpotSubscribers");
            
            logger.LogInformation("User {UserId} disconnected from JackpotHub. ConnectionId: {ConnectionId}", 
                userIdResult.Value, Context.ConnectionId);
            
            if (exception != null)
            {
                logger.LogWarning(exception, "User {UserId} disconnected with error", userIdResult.Value);
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
