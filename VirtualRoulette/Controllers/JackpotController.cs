/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Applications.Jackpot;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Hubs;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Controllers;

/// <summary>
/// Controller for jackpot operations.
/// </summary>
[Authorize]
[Route("api/v1/Jackpot")]
[ApiController]
public class JackpotController(
    IJackpotService jackpotService,
    IHubContext<JackpotHub> hubContext,
    ILogger<JackpotController> logger) : ControllerBase
{
    [HttpGet("current")]
    public ActionResult<ApiServiceResponse<long>> GetCurrentJackpot()
    {
        var result = jackpotService.GetCurrentJackpot();
        var response = result.ToApiResponse(result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
        return result.IsSuccess 
            ? Ok(response)
            : StatusCode(StatusCodes.Status500InternalServerError, response);
    }
    
    [HttpPost("increase")]
    public async Task<ActionResult<ApiServiceResponse<long>>> IncreaseJackpot([FromBody] IncreaseJackpotRequest request)
    {
        if (request.AmountInCents <= 0)
        {
            var error = new Error("Jackpot.InvalidAmount", "Amount must be greater than zero.", ErrorType.Validation);
            var result = Result.Failure<long>(error);
            var response = result.ToApiResponse(StatusCodes.Status400BadRequest);
            return BadRequest(response);
        }

        // Convert cents to internal format (1 cent = 10,000)
        const int centToInternalMultiplier = 10_000;
        long amountInInternalFormat = request.AmountInCents * centToInternalMultiplier;
        
        // Increase the jackpot
        var increaseResult = jackpotService.IncreaseJackpot(amountInInternalFormat);
        
        if (increaseResult.IsFailure)
        {
            var errorResponse = increaseResult.ToApiResponse(StatusCodes.Status500InternalServerError);
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
        
        var newJackpot = increaseResult.Value;
        
        // Broadcast the update to all connected clients via SignalR
        await hubContext.Clients.Group("JackpotSubscribers").SendAsync("JackpotUpdated", newJackpot);
        
        logger.LogInformation("Jackpot increased by {AmountInCents} cents (internal: {InternalAmount}). New jackpot: {NewJackpot}", 
            request.AmountInCents, amountInInternalFormat, newJackpot);
        
        var successResponse = increaseResult.ToApiResponse(StatusCodes.Status200OK);
        return Ok(successResponse);
    }
}
*/
