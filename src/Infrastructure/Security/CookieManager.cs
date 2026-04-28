using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure.Security;

public sealed class CookieManager(IHttpContextAccessor httpContextAccessor, IOptions<CookieSettings> options) : ICookieManager
{
    private readonly CookieSettings _settings = options.Value;

    public void SetRefreshToken(string refreshToken)
    {
        var response = httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("No active HTTP context.");
        response.Cookies.Append(_settings.RefreshTokenCookieName, refreshToken, BuildCookieOptions());
    }

    public string? GetRefreshToken()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return null;
        }

        request.Cookies.TryGetValue(_settings.RefreshTokenCookieName, out var token);
        return token;
    }

    public void ClearRefreshToken()
    {
        var response = httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("No active HTTP context.");
        response.Cookies.Delete(_settings.RefreshTokenCookieName);
    }

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = _settings.Secure,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenDays)
    };
}
