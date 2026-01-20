using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Persistence.Repositories;

public interface IBetRepository
{
    Task<Result<Bet>> CreateAsync(Bet bet);
}

public class BetRepository(AppDbContext dbContext) : IBetRepository
{
    public async Task<Result<Bet>> CreateAsync(Bet bet)
    {
        try
        {
            dbContext.Bets.Add(bet);
            // SaveChanges will be called by the service within transaction
            return Result.Success(bet);
        }
        catch (Exception e)
        {
            return Result.Failure<Bet>(DomainError.DbError.Error(nameof(BetRepository), e.Message));
        }
    }
}
