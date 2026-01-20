using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.Bet;
using VirtualRoulette.Common;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[Authorize]
[Route("api/v1/Roulette")]
[ApiController]
public class RouletteController(IRouletteService rouletteService) : ControllerBase
{
    [HttpPost("bet")]
    public async Task<ActionResult<ApiServiceResponse<BetResponse>>> Bet([FromBody] string bet)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdClaim, out var id) ? id : 0;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        var result = await rouletteService.Bet(bet, userId, ipAddress);
        
        return result.IsSuccess 
            ? Ok(result.ToApiResponse())
            : BadRequest(result.ToApiResponse());
    }
}