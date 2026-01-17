using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VirtualRoulette.Applications.Authorization;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[EnableRateLimiting("fixed")]
[Route("api/v1/Authorize")]
[ApiController]
public class AuthorizationController(IAuthorizationService authorizationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await authorizationService.Register(request.Username, request.Password);

        return result.IsFailure
            ? BadRequest(new AuthResponse
            {
                Success = false,
                Message = result.Error
            })
            : CreatedAtAction(nameof(Register), new AuthResponse
            {
                Success = true,
                Message = "User registered successfully."
            });
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
    {
        var result = await authorizationService.SignIn(request.Username, request.Password, HttpContext);
        
        if (result.IsFailure)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = result.Error
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Signed in successfully."
        });
    }

    [HttpPost("signOut")]
    public new async Task<ActionResult<AuthResponse>> SignOut()
    {
        await authorizationService.SignOut(HttpContext);
        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Signed out successfully."
        });
    }
}