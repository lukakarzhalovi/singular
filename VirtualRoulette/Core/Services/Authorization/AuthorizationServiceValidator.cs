using VirtualRoulette.Shared;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Core.Services.Authorization;

public class AuthorizationServiceValidator(IUserRepository userRepository)
{
    public async Task<Result> ValidateRegistrationAsync(string username, string password)
    {
        var usernameResult = ValidateUsername(username);
        if (usernameResult.IsFailure)
        {
            return Result.Failure(usernameResult.Errors);
        }

        var passwordResult = ValidatePassword(password);
        if (passwordResult.IsFailure)
        {
            return Result.Failure(passwordResult.Errors);
        }

        var usernameExistsResult = await userRepository.UsernameExistsAsync(username);
        if (usernameExistsResult.IsFailure)
        {
            return Result.Failure(usernameExistsResult.Errors);
        }

        return usernameExistsResult.Value 
            ? Result.Failure("Username already exists.") 
            : Result.Success();
    }

    public static Result ValidateSignIn(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure("Username and password are required.");
        }

        return Result.Success();
    }
    
    private static Result ValidateUsername(string username)
    {
        return string.IsNullOrWhiteSpace(username) 
            ? Result.Failure("Username is required.") 
            : Result.Success();
    }

    private static Result ValidatePassword(string password)
    {
        return string.IsNullOrWhiteSpace(password)
            ? Result.Failure("Password is required.") 
            : Result.Success();
    }
}
