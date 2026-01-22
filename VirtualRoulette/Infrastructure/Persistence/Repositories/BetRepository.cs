using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Shared;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Pagination;

namespace VirtualRoulette.Infrastructure.Persistence.Repositories;

public interface IBetRepository
{
    Task<Result<Bet>> CreateAsync(Bet bet);
    Task<Result<PagedList<Bet>>> GetByUserId(int userId, int skip, int limit);
}

public class BetRepository(AppDbContext context) : BaseRepository(context), IBetRepository
{
    public async Task<Result<Bet>> CreateAsync(Bet bet)
    {
        try
        {
            await Context.Bets.AddAsync(bet);
            return Result.Success(bet);
        }
        catch (Exception e)
        {
            return Result.Failure<Bet>(DomainError.DbError.Error(nameof(BetRepository), e.Message));
        }
    }

    public async Task<Result<PagedList<Bet>>> GetByUserId(int userId, int skip, int limit)
    {
        try
        {
            var query = Context.Bets.Where(b => b.UserId == userId).AsNoTracking();
            
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var pagedList = new PagedList<Bet>
            {
                Items = items,
                TotalCount = items.Count
            };
            
            return Result.Success(pagedList);
        }
        catch (Exception e)
        {
            return Result.Failure<PagedList<Bet>>(DomainError.DbError.Error(nameof(BetRepository), e.Message));
        }
    }
}
