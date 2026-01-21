using Microsoft.Extensions.Caching.Memory;

namespace VirtualRoulette.Applications.ActivityTracker;

public interface IUserActivityTracker
{
    void UpdateActivity(int userId);
    bool IsUserActive(int userId);
    void RemoveUser(int userId);
    DateTime? GetLastActivity(int userId);
}

public class UserActivityTracker(IMemoryCache cache) : IUserActivityTracker
{
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "user_activity";

    public void UpdateActivity(int userId)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_inactivityTimeout);
        
        cache.Set(GetCacheKey(userId), DateTime.UtcNow, cacheOptions);
    }
    
    public bool IsUserActive(int userId)
    {
        
        if (cache.TryGetValue(GetCacheKey(userId), out DateTime lastActivity))
        {
            return DateTime.UtcNow - lastActivity < _inactivityTimeout;
        }
        
        return false;
    }
    
    public void RemoveUser(int userId)
    {
        cache.Remove(GetCacheKey(userId));
    }


    public DateTime? GetLastActivity(int userId)
    {
        
        if (cache.TryGetValue(GetCacheKey(userId), out DateTime lastActivity))
        {
            return lastActivity;
        }
        
        return null;
    }

    private static string GetCacheKey(int userId) => $"{CacheKeyPrefix}{userId}";
}
