using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VirtualRoulette.Applications.ActivityTracker;
using VirtualRoulette.Applications.PasswordHasher;
using VirtualRoulette.Common;
using VirtualRoulette.Models.Entities;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.Authorization;

public interface IAuthorizationService
{
    Task<Result> Register(string username, string password);
    
    Task<Result> SignIn(string username, string password, HttpContext httpContext);
    
    Task SignOut(HttpContext httpContext);
}

public class AuthorizationService(
    IUserRepository userRepository, 
    IPasswordHasherService passwordHasherService,
    AuthorizationServiceValidator validator,
    IUserActivityTracker activityTracker) : IAuthorizationService
{
    public async Task<Result> Register(string username, string password)
    {
        // Validate input
        var validationResult = await validator.ValidateRegistrationAsync(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.Error);
        }

        // Hash password and create user
        var passwordHash = passwordHasherService.HashPassword(password);
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            Balance = 0
        };

        var createResult = await userRepository.CreateAsync(user);
        
        return createResult.IsFailure 
            ? Result.Failure(createResult.Error) 
            : Result.Success();
    }

    public async Task<Result> SignIn(string username, string password, HttpContext httpContext)
    {
        // Validate input
        var validationResult = validator.ValidateSignIn(username, password);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.Error);
        }

        var getUserResult = await userRepository.GetByUsernameAsync(username);
        if (getUserResult.IsFailure)
        {
            return Result.Failure(getUserResult.Error);
        }

        var user = getUserResult.Value;
        if (!passwordHasherService.VerifyPassword(password, user.PasswordHash))
        {
            return Result.Failure("Invalid username or password.");
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

        // Sign in user
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        activityTracker.UpdateActivity(user.Id.ToString());

        return Result.Success();
    }
    
    public async Task SignOut(HttpContext httpContext)
    {
        // Get user ID before signing out
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Sign out from cookie authentication
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Remove user from activity tracking
        if (userId != null)
        {
            activityTracker.RemoveUser(userId);
        }
    }
}