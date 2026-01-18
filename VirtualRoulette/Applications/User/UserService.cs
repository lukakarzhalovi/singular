using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.User;

public interface IUserService
{
    Task<Result<decimal>> GetBalance(int userId);
    
    Task<Result> AddBalance(int userId, decimal amount);
}

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<Result<decimal>> GetBalance(int userId)
    {
        var userResult = await userRepository.GetById(userId);

        if (userResult.IsFailure)
        {
            return Result.Failure<decimal>(userResult.Errors);
        }

        var user = userResult.Value;

        if (user is null)
        {
            return Result.Failure<decimal>(DomainError.User.NotFound);
        }

        return Result.Success(user.Balance);
    }

    public async Task<Result> AddBalance(int userId, decimal amount)
    {
        var addResult = await userRepository.AddBalance(userId, amount);

        if (addResult.IsFailure)
        {
            return Result.Failure<decimal>(addResult.Errors);
        }

        return addResult.IsFailure
            ? Result.Failure<decimal>(addResult.Errors)
            : Result.Success();
    }
}