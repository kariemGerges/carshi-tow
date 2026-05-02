using CarshiTow.Api.DTOs;
using CarshiTow.Api.Mappings;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CarshiTow.Api.Controllers;

// AuthController is responsible for handling authentication and authorization requests.
// It provides endpoints for user registration, login, OTP verification, refresh token, and logout.
// It uses the AuthService to handle the authentication and authorization logic.
// It uses the RefreshTokenService to handle the refresh token logic.
// It uses the CookieManager to handle the cookie logic.
// It uses the UserRepository to handle the user logic.
// It uses the CookieSettings to handle the cookie settings.
// It uses the RateLimitingPolicies to handle the rate limiting logic.

[ApiController]

[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthService authService,
    IRefreshTokenService refreshTokenService,
    ICookieManager cookieManager,
    IUserRepository userRepository,
    IJwtAccessTokenValidator jwtAccessTokenValidator,
    IOptions<CookieSettings> cookieSettings) : ControllerBase
{
    // Health endpoint is used to check if the auth API is running.
    [HttpGet("health")]
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Health() => Ok("CarshiTow Auth API is running");

    // Request password reset endpoint is used to request a password reset.
    [HttpPost("password/reset-request")]
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.PasswordResetRequestPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.ToApp(), cancellationToken);
        return NoContent();
    }

    // Complete password reset endpoint is used to complete a password reset.
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompletePasswordReset([FromBody] PasswordResetCompleteDto request, CancellationToken cancellationToken)
    {
        await authService.CompletePasswordResetAsync(request.ToApp(), cancellationToken);
        return NoContent();
    }

    // Get current profile endpoint is used to get the current user's profile.
    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var profile = await authService.GetProfileAsync(userId, cancellationToken);
        return Ok(profile.ToApi());
    }

    // Register endpoint is used to register a new user.
    [HttpPost("register")] // POST /api/v1/auth/register
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.AuthPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request.ToApp(), cancellationToken);
        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is not null)
        {
            var refresh = await refreshTokenService.CreateAsync(user.Id, cancellationToken);
            cookieManager.SetRefreshToken(refresh.RawToken);
            Response.Cookies.Append(cookieSettings.Value.CsrfCookieName, response.CsrfToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = cookieSettings.Value.Secure,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.Value.RefreshTokenDays)
            });
        }

        return Ok(response.ToApi());
    }

    // Login endpoint is used to login a user.
    [HttpPost("login")] // POST /api/v1/auth/login
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.LoginPolicy)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var fingerprint = HttpContext.Items["DeviceFingerprint"]?.ToString() ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await authService.LoginAsync(request.ToApp(), fingerprint, ipAddress, cancellationToken);
        if (response.RequiresMfa)
        {
            return Ok(response.ToApi());
        }

        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is not null && response.CsrfToken is not null)
        {
            var refresh = await refreshTokenService.CreateAsync(user.Id, cancellationToken);
            cookieManager.SetRefreshToken(refresh.RawToken);
            Response.Cookies.Append(cookieSettings.Value.CsrfCookieName, response.CsrfToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = cookieSettings.Value.Secure,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.Value.RefreshTokenDays)
            });
        }

        return Ok(response.ToApi());
    }

    [HttpPost("mfa/verify")] // POST /api/v1/auth/mfa/verify
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.OtpPolicy)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> VerifyMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        if (!jwtAccessTokenValidator.TryGetUserId(request.MfaAccessToken, out var userId))
        {
            return Unauthorized();
        }

        var verify = new VerifyOtpRequest(request.Code, request.Purpose);
        var fingerprint = HttpContext.Items["DeviceFingerprint"]?.ToString() ?? string.Empty;
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await authService.VerifyOtpAsync(userId, verify, fingerprint, userAgent, ipAddress, cancellationToken);

        var refresh = await refreshTokenService.CreateAsync(userId, cancellationToken);
        cookieManager.SetRefreshToken(refresh.RawToken);
        Response.Cookies.Append(cookieSettings.Value.CsrfCookieName, response.CsrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = cookieSettings.Value.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.Value.RefreshTokenDays)
        });

        return Ok(response.ToApi());
    }

    [HttpPost("otp/verify")] // POST /api/v1/auth/otp/verify
    [Authorize]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.OtpPolicy)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var fingerprint = HttpContext.Items["DeviceFingerprint"]?.ToString() ?? string.Empty;
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await authService.VerifyOtpAsync(userId, request.ToApp(), fingerprint, userAgent, ipAddress, cancellationToken);

        var refresh = await refreshTokenService.CreateAsync(userId, cancellationToken);
        cookieManager.SetRefreshToken(refresh.RawToken);
        Response.Cookies.Append(cookieSettings.Value.CsrfCookieName, response.CsrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = cookieSettings.Value.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.Value.RefreshTokenDays)
        });

        return Ok(response.ToApi());
    }

    // Refresh endpoint is used to refresh a user's token.
    [HttpPost("refresh")] // POST /api/v1/auth/refresh
    [AllowAnonymous]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.RefreshPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieManager.GetRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        Request.Cookies.TryGetValue(cookieSettings.Value.CsrfCookieName, out var csrfCookie);
        var csrfHeader = Request.Headers["X-CSRF-TOKEN"].ToString();
        var effectiveRequest = string.IsNullOrWhiteSpace(csrfHeader) ? request : request with { CsrfToken = csrfHeader };
        var refreshResult = await authService.RefreshAsync(effectiveRequest.ToApp(), refreshToken, csrfCookie ?? string.Empty, cancellationToken);
        cookieManager.SetRefreshToken(refreshResult.NewRefreshToken);

        Response.Cookies.Append(cookieSettings.Value.CsrfCookieName, refreshResult.Auth.CsrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = cookieSettings.Value.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.Value.RefreshTokenDays)
        });
        return Ok(refreshResult.Auth.ToApi());
    }

    [HttpPost("logout")] // POST /api/v1/auth/logout    
    [Authorize]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.RefreshPolicy)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(cookieSettings.Value.CsrfCookieName, out var csrfCookie);
        var csrfHeader = Request.Headers["X-CSRF-TOKEN"].ToString();
        if (string.IsNullOrWhiteSpace(csrfCookie) || !string.Equals(csrfCookie, csrfHeader, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var refreshToken = cookieManager.GetRefreshToken();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await refreshTokenService.ValidateAsync(refreshToken, cancellationToken);
            if (token is not null && token.IsActive)
            {
                await refreshTokenService.RevokeAsync(token, cancellationToken);
            }
        }

        cookieManager.ClearRefreshToken();
        Response.Cookies.Delete(cookieSettings.Value.CsrfCookieName);
        return NoContent();
    }
}
