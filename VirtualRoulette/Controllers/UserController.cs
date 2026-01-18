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
        
        return balanceResult.IsSuccess 
            ? Ok(balanceResult.ToApiResponse())
            : BadRequest(balanceResult.ToApiResponse());
    }
}