using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Helpers;

namespace VirtualRoulette.Presentation.Filters;

public class RequireUserIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var filterSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<FilterSettings>>().Value;
        
        var userIdResult = UserHelper.GetUserId(context.HttpContext);
        
        if (userIdResult.IsFailure)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[filterSettings.UserIdKey] = userIdResult.Value;
        
        base.OnActionExecuting(context);
    }
}

public class RequireIpAddressAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var filterSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<FilterSettings>>().Value;
        
        var ipAddressResult = UserHelper.GetIpAddress(context.HttpContext);
        
        if (ipAddressResult.IsFailure)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[filterSettings.IpAddressKey] = ipAddressResult.Value;
        
        base.OnActionExecuting(context);
    }
}
