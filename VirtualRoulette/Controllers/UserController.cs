using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.User;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Pagination;
using VirtualRoulette.Filters;
using VirtualRoulette.Models.DTOs;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Controllers;

[Authorize]
[RequireUserId]
[Route("api/v1/User")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<ActionResult<ApiServiceResponse<long>>> GetBalance()
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var balanceResult = await userService.GetBalance(userId);
        return balanceResult.ToActionResult();
    }
    
    [HttpGet("bets")]
    public async Task<ActionResult<ApiServiceResponse<PagedList<Bet>>>> GetBets(int page, int limit)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var betsResult = await userService.GetBets(userId, page, limit);
        return betsResult.ToActionResult();
    }
    
    [HttpPost("balance")]
    public async Task<ActionResult<ApiServiceResponse>> AddBalance(long amountInCents)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var result = await userService.AddBalance(userId, amountInCents);
        return result.ToActionResult();
    }
}