using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VirtualRoulette.Configuration.Settings;

namespace VirtualRoulette.Applications.ActivityTracker;

public interface IUserActivityTracker
{
    void UpdateActivity(int userId);
    bool IsUserActive(int userId);
    void RemoveUser(int userId);
}

public class UserActivityTracker(
    IMemoryCache cache,
    IOptions<ActivityTrackingSettings> settings) : IUserActivityTracker
{
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(settings.Value.InactivityTimeoutMinutes);
    private readonly string _cacheKeyPrefix = settings.Value.CacheKeyPrefix;

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

    private string GetCacheKey(int userId) => $"{_cacheKeyPrefix}{userId}";
}
