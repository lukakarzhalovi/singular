using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Common.Pagination;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.User;

public interface IUserService
{
    Task<Result<decimal>> GetBalance(int userId);

    Task<Result<PagedList<Models.Entities.Bet>>> GetBets(int userId, int page, int limit);
}

public class UserService(
    IUserRepository userRepository,
    IBetRepository betRepository
) : IUserService
{
    public async Task<Result<decimal>> GetBalance(int userId)
    {
        var userResult = await userRepository.GetById(userId);

        if (userResult.IsFailure)
        {
            return Result.Failure<decimal>(userResult.Errors);
        }

        var user = userResult.Value;

        return user is null 
            ? Result.Failure<decimal>(DomainError.User.NotFound) 
            : Result.Success(user.Balance);
    }
    
    public async Task<Result<PagedList<Models.Entities.Bet>>> GetBets(int userId, int page, int limit)
    {
        var userBetResult = await betRepository.GetByUserId(userId, page, limit);
        
        return userBetResult.IsFailure 
            ? Result.Failure<PagedList<Models.Entities.Bet>>(userBetResult.Errors) 
            : userBetResult.Value;
    }
}