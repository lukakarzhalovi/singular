using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VirtualRoulette.Presentation.Configuration.Settings;

namespace VirtualRoulette.Core.Services.ActivityTracker;

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

    /// <summary>
    /// Update user's last activity time (resets 5-minute timer)
    /// </summary>
    public void UpdateActivity(int userId)
    {
        // Cache entry expires after inactivity timeout (5 minutes)
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_inactivityTimeout);
        
        cache.Set(GetCacheKey(userId), DateTime.UtcNow, cacheOptions);
    }
    
    /// <summary>
    /// Check if user was active within last 5 minutes
    /// </summary>
    public bool IsUserActive(int userId)
    {
        if (cache.TryGetValue(GetCacheKey(userId), out DateTime lastActivity))
        {
            return DateTime.UtcNow - lastActivity < _inactivityTimeout;
        }
        
        return false;
    }
    
    /// <summary>
    /// Remove user from activity tracking (on sign out)
    /// </summary>
    public void RemoveUser(int userId)
    {
        cache.Remove(GetCacheKey(userId));
    }

    private string GetCacheKey(int userId) => $"{_cacheKeyPrefix}{userId}";
}
