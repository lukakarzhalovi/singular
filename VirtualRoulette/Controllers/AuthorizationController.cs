using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Common;
using VirtualRoulette.Filters;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[EnableRateLimiting("fixed")]
[Route("api/v1/Authorize")]
[ApiController]
public class AuthorizationController(IAuthorizationService authorizationService) : ControllerBase
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
    public new ActionResult<ApiServiceResponse> SignOut()
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var result = authorizationService.SignOut(userId);
        return result.ToActionResult();
    }
}