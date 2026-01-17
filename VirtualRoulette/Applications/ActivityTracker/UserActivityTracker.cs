using Microsoft.Extensions.Caching.Memory;

namespace VirtualRoulette.Applications.ActivityTracker;

public interface IUserActivityTracker
{
    void UpdateActivity(string userId);
    bool IsUserActive(string userId);
    void RemoveUser(string userId);
    DateTime? GetLastActivity(string userId);
}

public class UserActivityTracker(IMemoryCache cache) : IUserActivityTracker
{
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "user_activity";

    public void UpdateActivity(string userId)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_inactivityTimeout);
        
        cache.Set(GetCacheKey(userId), DateTime.UtcNow, cacheOptions);
    }
    
    public bool IsUserActive(string userId)
    {
        
        if (cache.TryGetValue(GetCacheKey(userId), out DateTime lastActivity))
        {
            return DateTime.UtcNow - lastActivity < _inactivityTimeout;
        }
        
        return false;
    }
    
    public void RemoveUser(string userId)
    {
        cache.Remove(GetCacheKey(userId));
    }


    public DateTime? GetLastActivity(string userId)
    {
        
        if (cache.TryGetValue(GetCacheKey(userId), out DateTime lastActivity))
        {
            return lastActivity;
        }
        
        return null;
    }

    private static string GetCacheKey(string userId) => $"{CacheKeyPrefix}{userId}";
}
