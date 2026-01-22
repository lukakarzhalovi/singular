using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VirtualRoulette.Core.Services.Authorization;
using VirtualRoulette.Shared;
using VirtualRoulette.Presentation.Filters;
using VirtualRoulette.Core.DTOs.Responses;
using VirtualRoulette.Core.DTOs.Requests;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Presentation.Controllers;

[EnableRateLimiting("fixed")]
[Route("api/v1/Authorize")]
[ApiController]
public class AuthorizationController(
    IAuthorizationService authorizationService,
    IOptions<FilterSettings> filterSettings) : ControllerBase
{
    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiServiceResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await authorizationService.Register(request.Username, request.Password);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sign in with username and password (creates authentication cookie)
    /// </summary>
    [HttpPost("signin")]
    public async Task<ActionResult<ApiServiceResponse>> SignIn([FromBody] SignInRequest request)
    {
        var result = await authorizationService.SignIn(request.Username, request.Password, HttpContext);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sign out current user (requires authentication)
    /// </summary>
    [HttpPost("signOut")]
    [RequireUserId]
    public new async Task<ActionResult<ApiServiceResponse>> SignOut()
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var result = await authorizationService.SignOut(userId, HttpContext);
        return result.ToActionResult();
    }
}