namespace CarshiTow.Api.DTOs;

public sealed record RegisterRequestDto(string Email, string Password, string PhoneNumber);
public sealed record LoginRequestDto(string Email, string Password, string? ClientId);
public sealed record RefreshTokenRequestDto(string CsrfToken);
public sealed record VerifyOtpRequestDto(string Code, string Purpose);
public sealed record DeviceFingerprintDto(string Fingerprint);
public sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string CsrfToken);
public sealed record LoginResponseDto(bool RequiresMfa, string? AccessToken, DateTime? AccessTokenExpiresAtUtc, string? CsrfToken);
