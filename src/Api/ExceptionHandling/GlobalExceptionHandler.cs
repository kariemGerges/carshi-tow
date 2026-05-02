using CarshiTow.Api.Middleware;
using CarshiTow.Api.Srs;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace CarshiTow.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return false;
        }

        var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var id) && id is string s
            ? s
            : httpContext.TraceIdentifier;

        var (status, title, detail, code) = MapException(exception, environment.IsDevelopment());
        IReadOnlyDictionary<string, string[]>? validationFields = null;
        if (exception is ValidationException vex)
        {
            validationFields = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray(), StringComparer.Ordinal);
        }

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception. Status={Status}, Code={Code}, CorrelationId={CorrelationId}, Path={Path}",
                status,
                code,
                correlationId,
                httpContext.Request.Path.Value);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Handled client error. Status={Status}, Code={Code}, CorrelationId={CorrelationId}, Path={Path}",
                status,
                code,
                correlationId,
                httpContext.Request.Path.Value);
        }

        httpContext.Response.StatusCode = status;

        if (SrsApiEnvelopePaths.ShouldApply(httpContext.Request.Path))
        {
            var error = new SrsEnvelopeError(code, detail, correlationId, validationFields);
            var envelope = new SrsEnvelope(false, null, error);
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);
            return true;
        }

        var problem = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                Type = "about:blank"
            },
            Exception = exception
        };

        problem.ProblemDetails.Extensions["code"] = code;
        if (environment.IsDevelopment())
        {
            problem.ProblemDetails.Extensions["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            problem.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;
        }

        await problemDetails.WriteAsync(problem);

        return true;
    }

    private static (int Status, string Title, string Detail, string Code) MapException(Exception exception, bool isDevelopment)
    {
        return exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                isDevelopment ? FormatValidationErrors(ex) : "One or more validation errors occurred.",
                "VALIDATION_FAILED"),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                isDevelopment ? exception.Message : "The request could not be authenticated.",
                "UNAUTHORIZED"),

            InvalidOperationException ex when ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                "Conflict",
                isDevelopment ? ex.Message : "The resource already exists.",
                "CONFLICT"),

            InvalidOperationException ex => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                isDevelopment ? ex.Message : "The request could not be processed.",
                "INVALID_OPERATION"),

            ArgumentException ex => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                isDevelopment ? ex.Message : "The request contained invalid arguments.",
                "INVALID_ARGUMENT"),

            KeyNotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Not found",
                isDevelopment ? ex.Message : "The requested resource was not found.",
                "NOT_FOUND"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                isDevelopment ? exception.Message : "An unexpected error occurred. Reference the correlation id when contacting support.",
                "INTERNAL_ERROR")
        };
    }

    private static string FormatValidationErrors(ValidationException ex) =>
        string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}
