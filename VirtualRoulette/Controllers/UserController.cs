using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.User;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Helpers;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[Authorize]
[Route("api/v1/User")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
        [HttpGet("balance")]
    public async Task<ActionResult<ApiServiceResponse<decimal>>> GetBalance()
    {
        var userIdResult = UserHelper.GetUserId(HttpContext);
        if (userIdResult.IsFailure)
        {
            return Unauthorized(userIdResult.ToApiResponse());
        }

        var balanceResult = await userService.GetBalance(userIdResult.Value);
        
        return balanceResult.IsSuccess 
            ? Ok(balanceResult.ToApiResponse())
            : BadRequest(balanceResult.ToApiResponse());
    }
    
    [HttpGet("bets")]
    public async Task<ActionResult<ApiServiceResponse<BetHistoryDto>>> GetBets()
    {
        var userIdResult = UserHelper.GetUserId(HttpContext);
        if (userIdResult.IsFailure)
        {
            return Unauthorized(userIdResult.ToApiResponse());
        }
        
        var balanceResult = await userService.GetBets(userIdResult.Value);
        
        return balanceResult.IsSuccess 
            ? Ok(balanceResult.ToApiResponse())
            : BadRequest(balanceResult.ToApiResponse());
    }
}