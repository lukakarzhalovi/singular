using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtualRoulette.Applications.Bet;
using VirtualRoulette.Common;
using VirtualRoulette.Configuration.Settings;
using VirtualRoulette.Filters;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[Authorize]
[RequireUserId]
[RequireIpAddress]
[Route("api/v1/Roulette")]
[ApiController]
public class RouletteController(
    IRouletteService rouletteService,
    IOptions<FilterSettings> filterSettings) : ControllerBase
{
    [HttpPost("bet")]
    public async Task<ActionResult<ApiServiceResponse<BetResponse>>> Bet([FromBody] string bet)
    {
        var userId = (int)HttpContext.Items[filterSettings.Value.UserIdKey]!;
        var ipAddress = (string)HttpContext.Items[filterSettings.Value.IpAddressKey]!;
        
        var result = await rouletteService.Bet(bet, userId, ipAddress);
        return result.ToActionResult();
    }
}