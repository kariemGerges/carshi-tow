namespace CarshiTow.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSecureHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            context.Response.Headers.TryAdd("X-XSS-Protection", "0");
            context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; base-uri 'self';");
            if (context.Request.IsHttps)
            {
                context.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
            await next();
        });
    }
}
