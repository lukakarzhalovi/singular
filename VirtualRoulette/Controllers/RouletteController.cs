using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.Bet;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Helpers;
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
        var userIdResult = UserHelper.GetUserId(HttpContext);
        if (userIdResult.IsFailure)
        {
            return Unauthorized(userIdResult.ToApiResponse());
        }
        
        var ipAddressResult = UserHelper.GetIpAddress(HttpContext);
        if (ipAddressResult.IsFailure)
        {
            return BadRequest(userIdResult.ToApiResponse());
        }
        
        var result = await rouletteService.Bet(bet, userIdResult.Value, ipAddressResult.Value);
        
        return result.IsSuccess 
            ? Ok(result.ToApiResponse())
            : BadRequest(result.ToApiResponse());
    }
}