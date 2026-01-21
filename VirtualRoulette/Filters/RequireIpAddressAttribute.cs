using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VirtualRoulette.Common.Helpers;

namespace VirtualRoulette.Filters;

public class RequireIpAddressAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var ipAddressResult = UserHelper.GetIpAddress(context.HttpContext);
        
        if (ipAddressResult.IsFailure)
        {
            context.Result = new UnauthorizedResult(); //temp
            return;
        }

        context.HttpContext.Items["IpAddress"] = ipAddressResult.Value;
        
        base.OnActionExecuting(context);
    }
}
