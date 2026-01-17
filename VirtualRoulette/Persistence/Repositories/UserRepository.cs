using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Common;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Persistence.Repositories;

public interface IUserRepository
{
    Task<Result<User>> GetByUsernameAsync(string username);
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
                return Result.Failure<User>("User not found.");
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            return Result.Failure<User>($"Error retrieving user: {ex.Message}");
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
        catch (DbUpdateException ex)
        {
            return Result.Failure<User>($"Error creating user: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure<User>($"Unexpected error creating user: {ex.Message}");
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
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Error checking username existence: {ex.Message}");
        }
    }
}
