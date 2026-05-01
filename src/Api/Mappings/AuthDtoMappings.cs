using CarshiTow.Api.DTOs;
using CarshiTow.Application.DTOs;

namespace CarshiTow.Api.Mappings;

public static class AuthDtoMappings
{
    public static RegisterRequest ToApp(this RegisterRequestDto dto) =>
        new(
            dto.Email,
            dto.Password,
            dto.PhoneNumber,
            dto.BusinessName,
            dto.Abn,
            dto.AddressLine1,
            dto.Suburb,
            dto.State,
            dto.Postcode,
            dto.BusinessPhone,
            dto.VerificationDocumentUrls?.ToArray());
    public static LoginRequest ToApp(this LoginRequestDto dto) => new(dto.Email, dto.Password, dto.ClientId);
    public static RefreshTokenRequest ToApp(this RefreshTokenRequestDto dto) => new(dto.CsrfToken);
    public static VerifyOtpRequest ToApp(this VerifyOtpRequestDto dto) => new(dto.Code, dto.Purpose);

    public static RequestPasswordResetRequest ToApp(this PasswordResetRequestDto dto) => new(dto.Email);

    public static CompletePasswordResetRequest ToApp(this PasswordResetCompleteDto dto) => new(dto.Token, dto.NewPassword);

    public static AuthResponseDto ToApi(this AuthResponse r) => new(r.AccessToken, r.AccessTokenExpiresAtUtc, r.CsrfToken);
    public static LoginResponseDto ToApi(this LoginResponse r) => new(r.RequiresMfa, r.AccessToken, r.AccessTokenExpiresAtUtc, r.CsrfToken);

    public static UserProfileDto ToApi(this UserProfile p) =>
        new(p.Id, p.Email, p.PhoneNumber, p.Role, p.Status, p.IsMfaEnabled, p.Permissions, p.CreatedAtUtc, p.UpdatedAtUtc, p.LastLoginAtUtc);
}
