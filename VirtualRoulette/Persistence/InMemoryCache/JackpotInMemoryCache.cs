using Microsoft.Extensions.Caching.Memory;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;

namespace VirtualRoulette.Persistence.InMemoryCache;

public interface IJackpotInMemoryCache
{
    Result<long> Get();
    
    Result Set(long value);
    
    Result Add(long amount);
    
    Result Reset();
}

public class JackpotInMemoryCache(IMemoryCache cache) : IJackpotInMemoryCache
{
    private readonly object _lockObject = new();
    private const string CacheKey = "Jackpot";

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
    
    public Result Set(long value)
    {
        try
        {
            lock (_lockObject)
            {
                cache.Set(CacheKey, value);
                return Result.Success(value);
            }
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.InMemoryCache.Error(nameof(IJackpotInMemoryCache), e.Message));
        }
    }
    
    public Result Add(long amount)
    {
        try
        {
            lock (_lockObject)
            {
                var currentResult = Get();
                if (currentResult.IsFailure)
                {
                    return Result.Failure(currentResult.Errors);
                }

                var newValue = currentResult.Value + amount;
                cache.Set(CacheKey, newValue);
                return Result.Success();
            }
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.InMemoryCache.Error(nameof(IJackpotInMemoryCache), e.Message));
        }
    }
    
    public Result Reset()
    {
        try
        {
            lock (_lockObject)
            {
                var setResult = Set(0);
                
                if (setResult.IsFailure)
                {
                    return Result.Failure(setResult.Errors);
                }
                
                return Result.Success();
            }
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.InMemoryCache.Error(nameof(IJackpotInMemoryCache), e.Message));
        }
    }
}
