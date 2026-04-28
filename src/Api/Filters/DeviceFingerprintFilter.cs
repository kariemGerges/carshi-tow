using CarshiTow.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarshiTow.Api.Filters;

public sealed class DeviceFingerprintFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Items.ContainsKey("DeviceFingerprint"))
        {
            var correlationId = context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var id) && id is string s
                ? s
                : context.HttpContext.TraceIdentifier;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad request",
                Detail = "Missing device fingerprint.",
                Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                Type = "about:blank"
            };

            problem.Extensions["correlationId"] = correlationId;
            problem.Extensions["code"] = "DEVICE_FINGERPRINT_MISSING";

            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
