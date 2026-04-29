using System.Security.Cryptography;
using System.Text;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.Security;
using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace CarshiTow.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IInputSanitizer inputSanitizer,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IOtpService otpService,
    IDeviceService deviceService,
    ICsrfProtectionService csrfProtectionService,
    IBruteForceProtectionService bruteForceProtectionService,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IEmailSender emailSender,
    IOptions<PasswordResetSettings> passwordResetSettings) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = inputSanitizer.Sanitize(request.Email.Trim().ToLowerInvariant());
        var phone = inputSanitizer.Sanitize(request.PhoneNumber.Trim());
        var existing = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PhoneNumber = phone,
            Password = new HashedPassword(passwordHasher.Hash(request.Password))
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.Role, cancellationToken);
        var csrf = csrfProtectionService.GenerateToken();
        return new AuthResponse(accessToken, DateTime.UtcNow.AddMinutes(15), csrf);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string fingerprint, string ipAddress, CancellationToken cancellationToken)
    {
        var email = inputSanitizer.Sanitize(request.Email.Trim().ToLowerInvariant());
        await bruteForceProtectionService.EnsureLoginAllowedAsync(email, ipAddress, cancellationToken);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            await bruteForceProtectionService.RegisterLoginFailureAsync(email, ipAddress, cancellationToken);
            await Task.Delay(Random.Shared.Next(150, 350), cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!passwordHasher.Verify(request.Password, user.Password.Value))
        {
            await bruteForceProtectionService.RegisterLoginFailureAsync(email, ipAddress, cancellationToken);
            await Task.Delay(Random.Shared.Next(150, 350), cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await bruteForceProtectionService.ResetLoginFailuresAsync(email, ipAddress, cancellationToken);

        var known = await deviceService.IsKnownTrustedDeviceAsync(user.Id, fingerprint, cancellationToken);
        if (MfaLoginRules.ShouldChallengeSmsOtp(user, known))
        {
            await otpService.IssueAsync(user.Id, user.PhoneNumber, OtpPurpose.NewDeviceVerification, cancellationToken);
            var mfaToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.Role, cancellationToken);
            return new LoginResponse(true, mfaToken, DateTime.UtcNow.AddMinutes(15), null);
        }

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.Role, cancellationToken);
        return new LoginResponse(false, accessToken, DateTime.UtcNow.AddMinutes(15), csrfProtectionService.GenerateToken());
    }

    public async Task<RefreshAuthResult> RefreshAsync(RefreshTokenRequest request, string rawRefreshToken, string expectedCsrfToken, CancellationToken cancellationToken)
    {
        if (!csrfProtectionService.IsValid(expectedCsrfToken, request.CsrfToken))
        {
            throw new UnauthorizedAccessException("Invalid CSRF token.");
        }

        var refresh = await refreshTokenService.ValidateAsync(rawRefreshToken, cancellationToken) ??
                      throw new UnauthorizedAccessException("Invalid refresh token.");
        if (!refresh.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        var rotated = await refreshTokenService.RotateAsync(refresh, cancellationToken);
        var user = await userRepository.GetByIdAsync(rotated.Token.UserId, cancellationToken) ??
                   throw new UnauthorizedAccessException("User not found.");

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.Role, cancellationToken);
        var auth = new AuthResponse(accessToken, DateTime.UtcNow.AddMinutes(15), csrfProtectionService.GenerateToken());
        return new RefreshAuthResult(auth, rotated.RawToken);
    }

    public async Task<AuthResponse> VerifyOtpAsync(Guid userId, VerifyOtpRequest request, string fingerprint, string userAgent, string ipAddress, CancellationToken cancellationToken)
    {
        await bruteForceProtectionService.EnsureOtpAllowedAsync(userId, ipAddress, cancellationToken);

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
        {
            throw new InvalidOperationException("Invalid OTP purpose.");
        }

        var verified = await otpService.VerifyAsync(userId, request.Code, purpose, cancellationToken);
        if (!verified)
        {
            await bruteForceProtectionService.RegisterOtpFailureAsync(userId, ipAddress, cancellationToken);
            throw new UnauthorizedAccessException("Invalid OTP.");
        }

        await bruteForceProtectionService.ResetOtpFailuresAsync(userId, ipAddress, cancellationToken);
        await deviceService.UpsertDeviceAsync(userId, fingerprint, userAgent, ipAddress, trust: true, cancellationToken);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ??
                   throw new UnauthorizedAccessException("User not found.");

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.Role, cancellationToken);
        return new AuthResponse(accessToken, DateTime.UtcNow.AddMinutes(15), csrfProtectionService.GenerateToken());
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var email = inputSanitizer.Sanitize(request.Email.Trim().ToLowerInvariant());
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
            return;
        }

        await passwordResetTokenRepository.InvalidateActiveForUserAsync(user.Id, cancellationToken);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var tokenEntity = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashResetToken(rawToken),
            ExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(Math.Clamp(passwordResetSettings.Value.TokenExpirationMinutes, 5, 24 * 60))
        };

        await passwordResetTokenRepository.AddAsync(tokenEntity, cancellationToken);
        await passwordResetTokenRepository.SaveChangesAsync(cancellationToken);

        var baseUrl = passwordResetSettings.Value.ResetLinkBaseUrl.TrimEnd('/');
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var link =
            $"{baseUrl}{separator}token={Uri.EscapeDataString(rawToken)}";

        await emailSender.SendAsync(
            user.Email,
            "Reset your CarshiTow password",
            $"Use this link to set a new password (expires soon):\n\n{link}",
            cancellationToken);
    }

    public async Task CompletePasswordResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        var trimmed = request.Token.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new UnauthorizedAccessException("Invalid or expired reset token.");
        }

        var hash = HashResetToken(trimmed);
        var record = await passwordResetTokenRepository.GetActiveByHashAsync(hash, cancellationToken);
        if (record is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset token.");
        }

        var user = await userRepository.GetByIdAsync(record.UserId, cancellationToken) ??
                   throw new UnauthorizedAccessException("Invalid or expired reset token.");

        user.Password = new HashedPassword(passwordHasher.Hash(request.NewPassword));
        user.UpdatedAtUtc = DateTime.UtcNow;

        record.UsedAtUtc = DateTime.UtcNow;

        await refreshTokenService.RevokeAllActiveForUserAsync(user.Id, cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);
        await passwordResetTokenRepository.UpdateAsync(record, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfile> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ??
                   throw new KeyNotFoundException("User not found.");

        var permissions = RolePermissions.ForRole(user.Role).Order(StringComparer.Ordinal).ToArray();
        return new UserProfile(
            user.Id,
            user.Email,
            user.PhoneNumber,
            user.Role.ToString(),
            user.Status.ToString(),
            user.IsMfaEnabled,
            permissions,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            user.LastLoginAtUtc);
    }

    private static string HashResetToken(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
