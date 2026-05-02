using System.Text.Json;
using CarshiTow.Api.Middleware;
using CarshiTow.Api.Srs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CarshiTow.Api.Filters;

/// <summary>SRS §7.1 — wraps JSON API results as <c>{ success, data, error }</c>.</summary>
public sealed class SrsApiEnvelopeFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        Apply(context);
        return next();
    }

    private static void Apply(ResultExecutingContext context)
    {
        if (!SrsApiEnvelopePaths.ShouldApply(context.HttpContext.Request.Path))
        {
            return;
        }

        var correlationId = context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid) && cid is string s
            ? s
            : context.HttpContext.TraceIdentifier;

        switch (context.Result)
        {
            case FileResult:
                return;

            case NoContentResult:
                context.Result = new ObjectResult(Envelope.Success(null)) { StatusCode = StatusCodes.Status200OK };
                return;

            case NotFoundResult:
                context.Result = new ObjectResult(Envelope.Failure("NOT_FOUND", "The requested resource was not found.", correlationId))
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                return;

            case UnauthorizedResult:
                context.Result = new ObjectResult(Envelope.Failure("UNAUTHORIZED", "The request could not be authenticated.", correlationId))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;

            case ForbidResult:
                context.Result = new ObjectResult(Envelope.Failure("FORBIDDEN", "You are not allowed to perform this action.", correlationId))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;

            case BadRequestResult:
                context.Result = new ObjectResult(Envelope.Failure("BAD_REQUEST", "Bad request.", correlationId))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
                return;

            case StatusCodeResult scr:
                if (scr.StatusCode == StatusCodes.Status204NoContent)
                {
                    context.Result = new ObjectResult(Envelope.Success(null)) { StatusCode = StatusCodes.Status200OK };
                }

                return;

            case ObjectResult ox:
                WrapObjectResult(ox, correlationId);
                return;
        }
    }

    private static void WrapObjectResult(ObjectResult ox, string correlationId)
    {
        var status = ox.StatusCode
            ?? (ox is IStatusCodeActionResult sc ? sc.StatusCode : null)
            ?? StatusCodes.Status200OK;

        if (ox.Value is ProblemDetails pd)
        {
            ox.StatusCode ??= pd.Status ?? status;
            var code = ReadStringExtension(pd, "code") ?? ErrorCodeForStatus(ox.StatusCode ?? status);
            var message = string.IsNullOrWhiteSpace(pd.Detail) ? pd.Title ?? "Request failed" : pd.Detail;
            IReadOnlyDictionary<string, string[]>? fields = null;
            if (ox.Value is ValidationProblemDetails vpd && vpd.Errors.Count > 0)
            {
                fields = vpd.Errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal);
            }

            ox.Value = Envelope.Failure(code, message, correlationId, fields);
            return;
        }

        if (status >= 200 && status < 300)
        {
            ox.Value = Envelope.Success(ox.Value);
            return;
        }

        if (status >= 400)
        {
            var msg = ox.Value as string;
            if (string.IsNullOrWhiteSpace(msg) && status == StatusCodes.Status404NotFound)
            {
                msg = "The requested resource was not found.";
            }

            msg ??= "Request failed.";
            ox.Value = Envelope.Failure(ErrorCodeForStatus(status), msg, correlationId);
        }
    }

    private static string? ReadStringExtension(ProblemDetails pd, string key)
    {
        if (!pd.Extensions.TryGetValue(key, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string str => str,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => raw.ToString()
        };
    }

    private static string ErrorCodeForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "BAD_REQUEST",
        StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
        StatusCodes.Status403Forbidden => "FORBIDDEN",
        StatusCodes.Status404NotFound => "NOT_FOUND",
        StatusCodes.Status409Conflict => "CONFLICT",
        StatusCodes.Status503ServiceUnavailable => "SERVICE_UNAVAILABLE",
        _ => "ERROR"
    };

    private static class Envelope
    {
        public static SrsEnvelope Success(object? data) => new(true, data, null);

        public static SrsEnvelope Failure(
            string code,
            string message,
            string correlationId,
            IReadOnlyDictionary<string, string[]>? fields = null) =>
            new(false, null, new SrsEnvelopeError(code, message, correlationId, fields));
    }
}
