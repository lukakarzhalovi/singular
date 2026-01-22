using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Shared;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Infrastructure.Persistence.Repositories;

public interface IUserRepository
{
    Task<Result<User>> GetByUsernameAsync(string username);
    Task<Result<User?>> GetById(int id);
    Task<Result<User>> CreateAsync(User user);
    Task<Result<bool>> UsernameExistsAsync(string username);
    Task<Result<List<User>>> GetAllUsersAsync();
}

public class UserRepository(AppDbContext context) : BaseRepository(context), IUserRepository
{
    public async Task<Result<User>> GetByUsernameAsync(string username)
    {
        try
        {
            var user = await Context.Users
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
            var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
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
            await Context.Users.AddAsync(user);
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
            var exists = await Context.Users
                .AnyAsync(u => u.Username == username);
            return Result.Success(exists);
        }
        catch (Exception e)
        {
            return Result.Failure<bool>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }

    public async Task<Result<List<User>>> GetAllUsersAsync()
    {
        try
        {
            var users = await Context.Users.ToListAsync();
            return Result.Success(users);
        }
        catch (Exception e)
        {
            return Result.Failure<List<User>>(DomainError.DbError.Error(nameof(UserRepository), e.Message));
        }
    }
}
