using CarshiTow.Api.DTOs;
using CarshiTow.Application.DTOs;

namespace CarshiTow.Api.Mappings;

public static class AuthDtoMappings
{
    public static RegisterRequest ToApp(this RegisterRequestDto dto) => new(dto.Email, dto.Password, dto.PhoneNumber);
    public static LoginRequest ToApp(this LoginRequestDto dto) => new(dto.Email, dto.Password, dto.ClientId);
    public static RefreshTokenRequest ToApp(this RefreshTokenRequestDto dto) => new(dto.CsrfToken);
    public static VerifyOtpRequest ToApp(this VerifyOtpRequestDto dto) => new(dto.Code, dto.Purpose);

    public static AuthResponseDto ToApi(this AuthResponse r) => new(r.AccessToken, r.AccessTokenExpiresAtUtc, r.CsrfToken);
    public static LoginResponseDto ToApi(this LoginResponse r) => new(r.RequiresMfa, r.AccessToken, r.AccessTokenExpiresAtUtc, r.CsrfToken);
}
