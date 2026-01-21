using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.Bet;
using VirtualRoulette.Common;
using VirtualRoulette.Filters;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[Authorize]
[RequireUserId]
[RequireIpAddress]
[Route("api/v1/Roulette")]
[ApiController]
public class RouletteController(IRouletteService rouletteService) : ControllerBase
{
    [HttpPost("bet")]
    public async Task<ActionResult<ApiServiceResponse<BetResponse>>> Bet(string bet)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var ipAddress = (string)HttpContext.Items["IpAddress"]!;
        
        var result = await rouletteService.Bet(bet, userId, ipAddress);
        return result.ToActionResult();
    }
}