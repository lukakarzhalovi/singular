using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Core.Services.PasswordHasher;
using VirtualRoulette.Shared;
using VirtualRoulette.Infrastructure.SignalR;
using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;
using UserEntity = VirtualRoulette.Core.Entities.User;

namespace VirtualRoulette.Core.Services.Authorization;

public interface IAuthorizationService
{
    Task<Result> Register(string username, string password);
    
    Task<Result> SignIn(string username, string password, HttpContext httpContext);
    
    Task<Result> SignOut(int userId, HttpContext httpContext);
}

public class AuthorizationService(
    IUserRepository userRepository, 
    IPasswordHasherService passwordHasherService,
    AuthorizationServiceValidator validator,
    IUserActivityTracker activityTracker,
    IUnitOfWork unitOfWork,
    IHubContext<JackpotHub> hubContext,
    IJackpotHubConnectionTracker connectionTracker,
    IOptions<SignalRSettings> signalRSettings) : IAuthorizationService
{
    public async Task<Result> Register(string username, string password)
    {
        // Check if username and password are valid, and username doesn't exist
        var validationResult = await validator.ValidateRegistrationAsync(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.FirstError);
        }

        // Hash the password before storing it
        var passwordHashResult = passwordHasherService.HashPassword(password);
        if (passwordHashResult.IsFailure)
        {
            return Result.Failure(passwordHashResult.FirstError);
        }
        
        // Create new user with zero balance
        var user = new UserEntity
        {
            Username = username,
            PasswordHash = passwordHashResult.Value,
            CreatedAt = DateTime.UtcNow,
            Balance = 0
        };

        var createResult = await userRepository.CreateAsync(user);
        if (createResult.IsFailure)
        {
            return Result.Failure(createResult.FirstError);
        }

        var saveResult = await unitOfWork.SaveChangesAsync();
        return saveResult.IsFailure 
            ? Result.Failure(saveResult.Errors) 
            : Result.Success();
    }

    public async Task<Result> SignIn(string username, string password, HttpContext httpContext)
    {
        // Basic validation - check if fields are not empty
        var validationResult = AuthorizationServiceValidator.ValidateSignIn(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.FirstError);
        }

        // Find user by username
        var getUserResult = await userRepository.GetByUsernameAsync(username);
        if (getUserResult.IsFailure)
        {
            return Result.Failure(getUserResult.FirstError);
        }

        var user = getUserResult.Value;
        // Verify password matches the stored hash
        var verifyResult = passwordHasherService.VerifyPassword(password, user.PasswordHash);
        if (verifyResult.IsFailure)
        {
            return Result.Failure(verifyResult.FirstError);
        }

        var verify = verifyResult.Value;
        if (!verify)
        {
            return Result.Failure(DomainError.User.InvalidUser);
        }

        // Create authentication cookie with user info
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Track user activity for auto sign-out after 5 minutes
        activityTracker.UpdateActivity(user.Id);

        return Result.Success();
    }
    
    public async Task<Result> SignOut(int userId, HttpContext httpContext)
    {
        // Disconnect user from SignalR jackpot hub if connected
        var connectionId = connectionTracker.GetConnection(userId);
        
        if (connectionId != null)
        {
            var settings = signalRSettings.Value;
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, settings.JackpotGroupName);
            await hubContext.Clients.Client(connectionId).SendAsync(settings.ForceDisconnectMethod);
            connectionTracker.RemoveConnection(userId);
        }
        
        // Clear authentication cookie
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Stop tracking user activity
        activityTracker.RemoveUser(userId);
        
        return Result.Success();
    }
}