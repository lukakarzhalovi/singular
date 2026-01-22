using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Shared;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Pagination;
using VirtualRoulette.Shared.Result;
using BetEntity = VirtualRoulette.Core.Entities.Bet;

namespace VirtualRoulette.Core.Services.User;

public interface IUserService
{
    Task<Result<long>> GetBalance(int userId);

    Task<Result<PagedList<BetEntity>>> GetBets(int userId, int page, int limit);
    
    Task<Result> AddBalance(int userId, long amountInCents);
    
    Task<Result<List<string>>> GetActiveUsersAsync();
}

public class UserService(
    IUserRepository userRepository,
    IBetRepository betRepository,
    IUnitOfWork unitOfWork,
    IUserActivityTracker activityTracker
) : IUserService
{
    public async Task<Result<long>> GetBalance(int userId)
    {
        var userResult = await userRepository.GetById(userId);

        if (userResult.IsFailure)
        {
            return Result.Failure<long>(userResult.Errors);
        }

        var user = userResult.Value;

        return user is null 
            ? Result.Failure<long>(DomainError.User.NotFound) 
            : Result.Success(user.Balance);
    }
    
    public async Task<Result<PagedList<BetEntity>>> GetBets(int userId, int page, int limit)
    {
        var skip = (page - 1) * limit;
        var userBetResult = await betRepository.GetByUserId(userId, skip, limit);
        
        return userBetResult.IsFailure 
            ? Result.Failure<PagedList<BetEntity>>(userBetResult.Errors) 
            : Result.Success(userBetResult.Value);
    }
    
    public async Task<Result> AddBalance(int userId, long amountInCents)
    {
        var userResult = await userRepository.GetById(userId);
        
        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Errors);
        }

        var user = userResult.Value;
        
        if (user is null)
        {
            return Result.Failure(DomainError.User.NotFound);
        }

        if (amountInCents <= 0)
        {
            return Result.Failure(DomainError.User.InvalidAmount);
        }

        user.Balance += amountInCents;
        
        var saveResult = await unitOfWork.SaveChangesAsync();
        
        return saveResult.IsFailure 
            ? Result.Failure(saveResult.Errors) 
            : Result.Success();
    }

    public async Task<Result<List<string>>> GetActiveUsersAsync()
    {
        var allUsersResult = await userRepository.GetAllUsersAsync();
        
        if (allUsersResult.IsFailure)
        {
            return Result.Failure<List<string>>(allUsersResult.Errors);
        }

        var activeUsernames = allUsersResult.Value
            .Where(user => activityTracker.IsUserActive(user.Id))
            .Select(user => user.Username)
            .OrderBy(username => username)
            .ToList();

        return Result.Success(activeUsernames);
    }
}