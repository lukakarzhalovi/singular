using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Common.Pagination;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.User;

public interface IUserService
{
    Task<Result<long>> GetBalance(int userId);

    Task<Result<PagedList<Models.Entities.Bet>>> GetBets(int userId, int page, int limit);
    
    Task<Result> AddBalance(int userId, long amountInCents);
}

public class UserService(
    IUserRepository userRepository,
    IBetRepository betRepository,
    IUnitOfWork unitOfWork
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
    
    public async Task<Result<PagedList<Models.Entities.Bet>>> GetBets(int userId, int page, int limit)
    {
        var skip = (page - 1) * limit;
        var userBetResult = await betRepository.GetByUserId(userId, skip, limit);
        
        return userBetResult.IsFailure 
            ? Result.Failure<PagedList<Models.Entities.Bet>>(userBetResult.Errors) 
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
}