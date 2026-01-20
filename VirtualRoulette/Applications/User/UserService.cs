using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Models.DTOs;
using VirtualRoulette.Persistence;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.User;

public interface IUserService
{
    Task<Result<decimal>> GetBalance(int userId);
    
    Task<Result> AddBalance(int userId, decimal amount);
    Task<Result<BetHistoryDto>> GetBets(int userId);
}

public class UserService(
    AppDbContext dbContext,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
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
            var saveResult = await unitOfWork.SaveChangesAsync();

            return saveResult.IsFailure 
                ? Result.Failure(saveResult.Errors) 
                : Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.DbError.Error(nameof(UserService), e.Message));
        }
    }

    public Task<Result<BetHistoryDto>> GetBets(int userId)
    {
        throw new NotImplementedException();
    }
}