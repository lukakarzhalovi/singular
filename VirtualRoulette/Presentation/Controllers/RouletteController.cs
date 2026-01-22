using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtualRoulette.Core.Services.Bet;
using VirtualRoulette.Presentation.Filters;
using VirtualRoulette.Core.DTOs.Responses;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Presentation.Controllers;

[Authorize]
[RequireUserId]
[RequireIpAddress]
[Route("api/v1/Roulette")]
[ApiController]
public class RouletteController(
    IRouletteService rouletteService,
    IOptions<FilterSettings> filterSettings) : ControllerBase
{
    /// <summary>
    /// Place a bet on the roulette wheel
    /// </summary>
    [HttpPost("bet")]
    public async Task<ActionResult<ApiServiceResponse<BetResponse>>> Bet([FromBody] string bet)
    {
        // Get user ID and IP address from request filters
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var ipAddress = (string)HttpContext.Items[filterSettings.Value.IpAddressKey]!;
        
        var result = await rouletteService.Bet(bet, userId, ipAddress);
        return result.ToActionResult();
    }
}