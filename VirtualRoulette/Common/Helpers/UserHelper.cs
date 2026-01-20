using System.Security.Claims;
using VirtualRoulette.Common.Errors;

namespace VirtualRoulette.Common.Helpers;

public static class UserHelper
{
    public static Result<int> GetUserId(HttpContext content)
    {
        try
        {
            var userIdClaim = content.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.TryParse(userIdClaim, out var id) ? id : 0;

            return Result.Success(userId);

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
                ? Result.Failure<string>(DomainError.User.NotFound) 
                : Result.Success(ipAddress);
        }
        catch (Exception)
        {
            return Result.Failure<string>(DomainError.User.NotFound);
        }
    }
}