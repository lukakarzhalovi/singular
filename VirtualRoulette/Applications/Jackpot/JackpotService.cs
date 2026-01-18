using VirtualRoulette.Common;
using VirtualRoulette.Persistence.InMemoryCache;

namespace VirtualRoulette.Applications.Jackpot;


public interface IJackpotService
{
    Result<long> GetCurrentJackpot();
    
    Result AddToJackpot(int contributionInCents);
    
    Result ResetJackpot();
    
    /*
    Result<long> IncreaseJackpot(long amountInInternalFormat);
*/
}

public class JackpotService(IJackpotInMemoryCache jackpotInMemoryCache) : IJackpotService
{
    private const int CentToInternalMultiplier = 10_000;

    public Result<long> GetCurrentJackpot()
    {
        return jackpotInMemoryCache.Get();
    }

    /// <summary>
    /// Adds 1% of the bet amount to the jackpot.
    /// </summary>
    /// <param name="contributionInCents">The bet amount in cents (1% will be added).</param>
    /// <returns>The new jackpot amount after adding the contribution.</returns>
    public Result AddToJackpot(int contributionInCents)
    {
        // Convert cents to internal format and calculate 1%
        // contribution in cents * 10,000 (to internal) / 100 (for 1%)
        // = contribution * 100 in internal format
        long contributionInInternal = contributionInCents * (CentToInternalMultiplier / 100);
        
        var result = jackpotInMemoryCache.Add(contributionInInternal);

        if (result.IsFailure)
        {
            //errror
        }
        return Result.Success();
    }

    public Result ResetJackpot()
    {
        var result =  jackpotInMemoryCache.Reset();
        
        return result.IsFailure
            ? Result.Failure(result.Errors)
            : Result.Success();
    }
    
    /*public Result IncreaseJackpot(long amountInInternalFormat)
    {
        var result =  jackpotInMemoryCache.Add(amountInInternalFormat);
        
        return result.IsFailure
            ? Result.Failure(result.Errors)
            : Result.Success();    }*/
}
