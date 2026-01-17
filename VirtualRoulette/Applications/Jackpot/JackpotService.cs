using Microsoft.Extensions.Caching.Memory;

namespace VirtualRoulette.Applications.Jackpot;


public interface IJackpotService
{
    long GetCurrentJackpot();
    
    long AddToJackpot(int contributionInCents);
    
    long ResetJackpot();
}

public class JackpotService : IJackpotService
{
    private readonly IMemoryCache _cache;
    private const string JackpotCacheKey = "jackpot_amount";
    private const int CentToInternalMultiplier = 10_000;

    private readonly object _lockObject = new();

    public JackpotService(IMemoryCache cache)
    {
        _cache = cache;
        
        if (!_cache.TryGetValue(JackpotCacheKey, out long _))
        {
            _cache.Set(JackpotCacheKey, 0L);
        }
    }
    
    public long GetCurrentJackpot()
    {
        return _cache.TryGetValue(JackpotCacheKey, out long jackpot) ? jackpot : 0;
    }

    /// <summary>
    /// Adds 1% of the bet amount to the jackpot.
    /// </summary>
    /// <param name="contributionInCents">The bet amount in cents (1% will be added).</param>
    /// <returns>The new jackpot amount after adding the contribution.</returns>
    public long AddToJackpot(int contributionInCents)
    {
        lock (_lockObject)
        {
            var currentJackpot = GetCurrentJackpot();
            
            // Convert cents to internal format and calculate 1%
            // contribution in cents * 10,000 (to internal) / 100 (for 1%)
            // = contribution * 100 in internal format
            long contributionInInternal = contributionInCents * (CentToInternalMultiplier / 100);
            
            var newJackpot = currentJackpot + contributionInInternal;
            
            _cache.Set(JackpotCacheKey, newJackpot);
            
            return newJackpot;
        }
    }

    public long ResetJackpot()
    {
        lock (_lockObject)
        {
            var previousJackpot = GetCurrentJackpot();
            _cache.Set(JackpotCacheKey, 0L);
            return previousJackpot;
        }
    }
}
