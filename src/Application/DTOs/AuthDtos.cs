namespace CarshiTow.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, string PhoneNumber);
public sealed record LoginRequest(string Email, string Password, string? ClientId);
public sealed record RefreshTokenRequest(string CsrfToken);
public sealed record VerifyOtpRequest(string Code, string Purpose);

public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string CsrfToken);
public sealed record RefreshAuthResult(AuthResponse Auth, string NewRefreshToken);
public sealed record LoginResponse(bool RequiresMfa, string? AccessToken, DateTime? AccessTokenExpiresAtUtc, string? CsrfToken);

public sealed record RequestPasswordResetRequest(string Email);

public sealed record CompletePasswordResetRequest(string Token, string NewPassword);

public sealed record UserProfile(
    Guid Id,
    string Email,
    string PhoneNumber,
    string Role,
    string Status,
    bool IsMfaEnabled,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastLoginAtUtc);
