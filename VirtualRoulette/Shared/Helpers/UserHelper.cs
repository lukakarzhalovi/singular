using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Shared.Errors;

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
                ? Result.Failure<int>(DomainError.User.NotFound) 
                : Result.Success(id);
        }
        catch (Exception)
        {
            return Result.Failure<int>(DomainError.User.Unauthorized);
        }
    }
    
    public static Result<int> GetUserId(HubCallerContext content)
    {
        try
        {
            var userIdClaim = content.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parsed = int.TryParse(userIdClaim, out var id);

            return !parsed 
                ? Result.Failure<int>(DomainError.User.NotFound) 
                : Result.Success(id);
        }
        catch (Exception)
        {
            return Result.Failure<int>(DomainError.User.Unauthorized);
        }
    }
    
    public static Result<string> GetIpAddress(HttpContext content)
    {
        try
        {
            var ipAddress = content.Connection.RemoteIpAddress?.ToString();
            
            return ipAddress is null 
                ? Result.Failure<string>(DomainError.User.IpAddressNotFound) 
                : Result.Success(ipAddress);
        }
        catch (Exception)
        {
            return Result.Failure<string>(DomainError.User.IpAddressNotFound);
        }
    }
}