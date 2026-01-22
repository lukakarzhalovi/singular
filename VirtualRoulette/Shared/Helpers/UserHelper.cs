using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;

namespace VirtualRoulette.Shared.Helpers;

public static class UserHelper
{
    public static Result<int> GetUserId(HttpContext content)
    {
        try
        {
            var userIdClaim = content.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parsed = int.TryParse(userIdClaim, out var id);

            return !parsed 
                ? Result.Result.Failure<int>(DomainError.User.NotFound) 
                : Result.Result.Success(id);
        }
        catch (Exception)
        {
            return Result.Result.Failure<int>(DomainError.User.Unauthorized);
        }
    }
    
    public static Result<int> GetUserId(HubCallerContext content)
    {
        try
        {
            var userIdClaim = content.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parsed = int.TryParse(userIdClaim, out var id);

            return !parsed 
                ? Result.Result.Failure<int>(DomainError.User.NotFound) 
                : Result.Result.Success(id);
        }
        catch (Exception)
        {
            return Result.Result.Failure<int>(DomainError.User.Unauthorized);
        }
    }
    
    public static Result<string> GetIpAddress(HttpContext content)
    {
        try
        {
            var ipAddress = content.Connection.RemoteIpAddress?.ToString();
            
            return ipAddress is null 
                ? Result.Result.Failure<string>(DomainError.User.IpAddressNotFound) 
                : Result.Result.Success(ipAddress);
        }
        catch (Exception)
        {
            return Result.Result.Failure<string>(DomainError.User.IpAddressNotFound);
        }
    }
}