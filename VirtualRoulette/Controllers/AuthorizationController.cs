using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Common;
using VirtualRoulette.Configuration.Settings;
using VirtualRoulette.Filters;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[EnableRateLimiting("fixed")]
[Route("api/v1/Authorize")]
[ApiController]
public class AuthorizationController(
    IAuthorizationService authorizationService,
    IOptions<FilterSettings> filterSettings) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiServiceResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await authorizationService.Register(request.Username, request.Password);
        return result.ToActionResult();
    }

    [HttpPost("signin")]
    public async Task<ActionResult<ApiServiceResponse>> SignIn([FromBody] SignInRequest request)
    {
        var result = await authorizationService.SignIn(request.Username, request.Password, HttpContext);
        return result.ToActionResult();
    }

    [HttpPost("signOut")]
    [RequireUserId]
    public new async Task<ActionResult<ApiServiceResponse>> SignOut()
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var result = await authorizationService.SignOut(userId, HttpContext);
        return result.ToActionResult();
    }
}