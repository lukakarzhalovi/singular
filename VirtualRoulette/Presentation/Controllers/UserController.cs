using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtualRoulette.Core.Services.User;
using VirtualRoulette.Shared;
using VirtualRoulette.Presentation.Filters;
using VirtualRoulette.Core.DTOs.Responses;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Pagination;

namespace VirtualRoulette.Presentation.Controllers;

[Authorize]
[RequireUserId]
[Route("api/v1/User")]
[ApiController]
public class UserController(
    IUserService userService,
    IOptions<FilterSettings> filterSettings) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<ActionResult<ApiServiceResponse<long>>> GetBalance()
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var balanceResult = await userService.GetBalance(userId);
        return balanceResult.ToActionResult();
    }
    
    [HttpGet("bets")]
    public async Task<ActionResult<ApiServiceResponse<PagedList<Bet>>>> GetBets(int page, int limit)
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var betsResult = await userService.GetBets(userId, page, limit);
        return betsResult.ToActionResult();
    }
    
    [HttpPost("balance")]
    public async Task<ActionResult<ApiServiceResponse>> AddBalance(long amountInCents)
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var result = await userService.AddBalance(userId, amountInCents);
        return result.ToActionResult();
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiServiceResponse<List<string>>>> GetActiveUsers()
    {
        var result = await userService.GetActiveUsersAsync();
        return result.ToActionResult();
    }
}