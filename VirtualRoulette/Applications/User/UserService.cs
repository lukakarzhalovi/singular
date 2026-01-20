using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Persistence;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.User;

public interface IUserService
{
    Task<Result<decimal>> GetBalance(int userId);
    
    Task<Result> AddBalance(int userId, decimal amount);
}

public class UserService(AppDbContext dbContext, IUserRepository userRepository) : IUserService
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
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Result.Failure(DomainError.User.NotFound);
            }

            user.Balance += amount;
            await dbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.DbError.Error(nameof(UserService), e.Message));
        }
    }
}