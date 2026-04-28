using System.IdentityModel.Tokens.Jwt;

namespace CarshiTow.Api.Middleware;

public sealed class JwtMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            context.Items["RawJwt"] = token;

            if (new JwtSecurityTokenHandler().CanReadToken(token))
            {
                var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
                context.Items["JwtSubject"] = parsed.Subject;
            }
        }

        await next(context);
    }
}
