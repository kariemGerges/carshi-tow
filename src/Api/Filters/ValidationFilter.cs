using CarshiTow.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarshiTow.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var correlationId = context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var id) && id is string s
                ? s
                : context.HttpContext.TraceIdentifier;

            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more fields failed validation.",
                Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                Type = "about:blank"
            };

            problem.Extensions["correlationId"] = correlationId;
            problem.Extensions["code"] = "MODEL_VALIDATION_FAILED";

            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        await next();
    }
}
