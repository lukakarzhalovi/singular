using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VirtualRoulette.Common.Helpers;

namespace VirtualRoulette.Filters;

public class RequireUserIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userIdResult = UserHelper.GetUserId(context.HttpContext);
        
        if (userIdResult.IsFailure)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["UserId"] = userIdResult.Value;
        
        base.OnActionExecuting(context);
    }
}
