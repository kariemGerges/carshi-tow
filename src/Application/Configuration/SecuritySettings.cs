namespace CarshiTow.Application.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
}

public sealed class TwilioSettings
{
    public const string SectionName = "Twilio";
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
}

public sealed class CookieSettings
{
    public const string SectionName = "Cookies";
    public string RefreshTokenCookieName { get; set; } = "refresh_token";
    public string CsrfCookieName { get; set; } = "csrf_token";
    public int RefreshTokenDays { get; set; } = 7;
    public bool Secure { get; set; } = true;
}
