using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Common;
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

        var response = result
            .ToApiResponse(result.IsSuccess ? StatusCodes.Status201Created : StatusCodes.Status400BadRequest);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(Register), response)
            : BadRequest(response);
    }

    [HttpPost("signin")]
    public async Task<ActionResult<ApiServiceResponse>> SignIn([FromBody] SignInRequest request)
    {
        var result = await authorizationService.SignIn(request.Username, request.Password, HttpContext);
        
        var response = result
            .ToApiResponse(result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status401Unauthorized);
        
        return result.IsSuccess 
            ? Ok(response)
            : Unauthorized(response);
    }

    [HttpPost("signOut")]
    public new async Task<ActionResult<ApiServiceResponse>> SignOut()
    {
        var result = await authorizationService.SignOut(HttpContext);
        var response = result.ToApiResponse(result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
        return result.IsSuccess 
            ? Ok(response)
            : StatusCode(StatusCodes.Status500InternalServerError, response);
    }
}