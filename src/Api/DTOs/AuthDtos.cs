namespace CarshiTow.Api.DTOs;

public sealed record RegisterRequestDto(string Email, string Password, string PhoneNumber);
public sealed record LoginRequestDto(string Email, string Password, string? ClientId);
public sealed record RefreshTokenRequestDto(string CsrfToken);
public sealed record VerifyOtpRequestDto(string Code, string Purpose);
public sealed record DeviceFingerprintDto(string Fingerprint);
public sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string CsrfToken);
public sealed record LoginResponseDto(bool RequiresMfa, string? AccessToken, DateTime? AccessTokenExpiresAtUtc, string? CsrfToken);

public sealed record PasswordResetRequestDto(string Email);

public sealed record PasswordResetCompleteDto(string Token, string NewPassword);

public sealed record UserProfileDto(
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
