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
        // Validate input
        var validationResult = await validator.ValidateRegistrationAsync(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.FirstError);
        }

        // Hash password and create user
        var passwordHashResult = passwordHasherService.HashPassword(password);
        if (passwordHashResult.IsFailure)
        {
            return Result.Failure(passwordHashResult.FirstError);
        }
        
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
        // Validate input
        var validationResult = AuthorizationServiceValidator.ValidateSignIn(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.FirstError);
        }

        var getUserResult = await userRepository.GetByUsernameAsync(username);
        if (getUserResult.IsFailure)
        {
            return Result.Failure(getUserResult.FirstError);
        }

        var user = getUserResult.Value;
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

        activityTracker.UpdateActivity(user.Id);

        return Result.Success();
    }
    
    public async Task<Result> SignOut(int userId, HttpContext httpContext)
    {
        // Get user's active connection
        var connectionId = connectionTracker.GetConnection(userId);
        
        if (connectionId != null)
        {
            var settings = signalRSettings.Value;
            // Remove from jackpot group
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, settings.JackpotGroupName);
            
            // Send disconnect signal to client
            await hubContext.Clients.Client(connectionId).SendAsync(settings.ForceDisconnectMethod);
            
            // Remove connection from tracker
            connectionTracker.RemoveConnection(userId);
        }
        
        // Sign out from cookie authentication
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Remove from activity tracker
        activityTracker.RemoveUser(userId);
        
        return Result.Success();
    }
}