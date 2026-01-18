using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualRoulette.Applications.User;
using VirtualRoulette.Common;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

[Authorize]
[Route("api/v1/User")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet("{userId:int}/balance")]
    public async Task<ActionResult<ApiServiceResponse<decimal>>> GetBalance([FromRoute] int userId)
    {
        var balanceResult = await userService.GetBalance(userId);

        var response = balanceResult.ToApiResponse(
            balanceResult.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        
        return balanceResult.IsSuccess 
            ? Ok(response)
            : BadRequest(response);
    }
}