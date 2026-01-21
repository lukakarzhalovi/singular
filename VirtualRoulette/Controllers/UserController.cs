using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.User;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Helpers;
using VirtualRoulette.Common.Pagination;
using VirtualRoulette.Models.DTOs;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Controllers;

[Authorize]
[Route("api/v1/User")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
        [HttpGet("balance")]
    public async Task<ActionResult<ApiServiceResponse<long>>> GetBalance()
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
    public async Task<ActionResult<ApiServiceResponse<PagedList<Bet>>>> GetBets(int page, int limit)
    {
        var userIdResult = UserHelper.GetUserId(HttpContext);
        if (userIdResult.IsFailure)
        {
            return Unauthorized(userIdResult.ToApiResponse());
        }
        
        var balanceResult = await userService.GetBets(userIdResult.Value, page, limit);
        
        return balanceResult.IsSuccess 
            ? Ok(balanceResult.ToApiResponse())
            : BadRequest(balanceResult.ToApiResponse());
    }
    
    [HttpPost("balance")]
    public async Task<ActionResult<ApiServiceResponse>> AddBalance([FromBody] long amountInCents)
    {
        var userIdResult = UserHelper.GetUserId(HttpContext);
        if (userIdResult.IsFailure)
        {
            return Unauthorized(userIdResult.ToApiResponse());
        }

        var result = await userService.AddBalance(userIdResult.Value, amountInCents);
        
        return result.IsSuccess 
            ? Ok(result.ToApiResponse())
            : BadRequest(result.ToApiResponse());
    }
}