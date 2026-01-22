using Microsoft.Extensions.Caching.Memory;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Infrastructure.Persistence.Caching;

public interface IJackpotInMemoryCache
{
    Result<long> Get();
    
    Result Set(long value);
}

public class JackpotInMemoryCache(IMemoryCache cache) : IJackpotInMemoryCache
{
    private readonly object _lockObject = new();
    private const string CacheKey = "Jackpot";

    /// <summary>
    /// Get current jackpot amount (returns 0 if not set)
    /// </summary>
    public Result<long> Get()
    {
        try
        {
            var value = cache.TryGetValue(CacheKey, out long result) ? result : 0;
            return Result.Success(value);
        }
        catch (Exception e)
        {
            return Result.Failure<long>(DomainError.InMemoryCache.Error(nameof(IJackpotInMemoryCache), e.Message));
        }
    }
    
    /// <summary>
    /// Update jackpot amount (thread-safe)
    /// </summary>
    public Result Set(long value)
    {
        try
        {
            // Use lock to prevent race conditions when multiple bets happen at once
            lock (_lockObject)
            {
                cache.Set(CacheKey, value);
                return Result.Success();
            }
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.InMemoryCache.Error(nameof(IJackpotInMemoryCache), e.Message));
        }
    }
}
