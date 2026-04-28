using CarshiTow.Application.Interfaces;

namespace CarshiTow.Api.Middleware;

public sealed class DeviceFingerprintMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDeviceFingerprintGenerator generator)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientId = context.Request.Headers["X-Client-Id"].ToString();

        context.Items["DeviceFingerprint"] = generator.Generate(userAgent, ip, string.IsNullOrWhiteSpace(clientId) ? null : clientId);
        await next(context);
    }
}
