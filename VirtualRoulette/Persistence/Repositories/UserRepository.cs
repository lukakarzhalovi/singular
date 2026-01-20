using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Persistence.Repositories;

public interface IUserRepository
{
    Task<Result<User>> GetByUsernameAsync(string username);
    Task<Result<User?>> GetById(int id);
    Task<Result<User>> CreateAsync(User user);
    Task<Result<bool>> UsernameExistsAsync(string username);
}

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<Result<User>> GetByUsernameAsync(string username)
    {
        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return Result.Failure<User>(DomainError.User.NotFound);
            }
            
            return Result.Success(user);
        }
        catch (Exception e)
        {
            return Result.Failure<User>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }

    public async Task<Result<User?>> GetById(int userId)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return Result.Success<User?>(user);
        }
        catch (Exception e)
        {
            return Result.Failure<User?>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }

    public async Task<Result<User>> CreateAsync(User user)
    {
        try
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return Result.Success(user);
        }
        catch (Exception e)
        {
            return Result.Failure<User>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }

    public async Task<Result<bool>> UsernameExistsAsync(string username)
    {
        try
        {
            var exists = await dbContext.Users
                .AnyAsync(u => u.Username == username);
            return Result.Success(exists);
        }
        catch (Exception e)
        {
            return Result.Failure<bool>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }
}
