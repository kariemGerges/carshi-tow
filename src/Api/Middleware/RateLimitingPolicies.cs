using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
namespace CarshiTow.Api.Middleware;

public static class RateLimitingPolicies
{
    public const string AuthPolicy = "auth-policy";
    public const string LoginPolicy = "auth-login-policy";
    public const string OtpPolicy = "auth-otp-policy";
    public const string RefreshPolicy = "auth-refresh-policy";
    public const string PasswordResetRequestPolicy = "auth-password-reset-request-policy";
    public const string DefaultPolicy = "default-policy";

    /// <summary>SRS §8.3 — photo upload (per authenticated user).</summary>
    public const string PhotoUploadPolicy = "photo-upload-policy";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(AuthPolicy, o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddPolicy(LoginPolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"login:{ip}".ToLowerInvariant();
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                });
            });

            options.AddPolicy(OtpPolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"otp:{ip}".ToLowerInvariant();
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                });
            });

            options.AddPolicy(RefreshPolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"refresh:{ip}".ToLowerInvariant();
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                });
            });

            options.AddPolicy(PasswordResetRequestPolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"pwd-reset-req:{ip}".ToLowerInvariant();
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0
                });
            });

            options.AddFixedWindowLimiter(DefaultPolicy, o =>
            {
                o.PermitLimit = 100;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddPolicy(PhotoUploadPolicy, ctx =>
            {
                var sub = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                          ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          "anonymous";
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"{sub}:{ip}".ToLowerInvariant();
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 50,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                var problemContext = new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "The request rate exceeds the configured limit. Retry after the delay indicated by the Retry-After header.",
                        Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                        Type = "about:blank"
                    }
                };

                problemContext.ProblemDetails.Extensions["code"] = "RATE_LIMIT_EXCEEDED";
                await problemDetailsService.WriteAsync(problemContext);
            };
        });

        return services;
    }
}
