using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CampaignService.Services;

namespace CampaignService.Attributes;

/// <summary>
/// Custom authorization attribute that validates JWT tokens with AuthService
/// </summary>
public class AuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authService = context.HttpContext.RequestServices.GetService<IAuthValidationService>();
        if (authService == null)
        {
            context.Result = new ObjectResult(new { message = "Authentication service not available" })
            {
                StatusCode = 500
            };
            return;
        }

        var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        var validationResult = await authService.GetUserFromAuthHeaderAsync(authHeader);

        if (validationResult == null)
        {
            context.Result = new ObjectResult(new { message = "Authorization header is required" })
            {
                StatusCode = 401
            };
            return;
        }

        if (!validationResult.IsValid)
        {
            context.Result = new ObjectResult(new { message = validationResult.ErrorMessage ?? "Invalid token" })
            {
                StatusCode = 401
            };
            return;
        }

        // Add user information to HttpContext for use in controllers
        context.HttpContext.Items["UserId"] = validationResult.UserId;
        context.HttpContext.Items["UserEmail"] = validationResult.Email;
        context.HttpContext.Items["UserFullName"] = validationResult.FullName;

        await next();
    }
}
