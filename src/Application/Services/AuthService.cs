using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Domain.ValueObjects;

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
    IBruteForceProtectionService bruteForceProtectionService) : IAuthService
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

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, cancellationToken);
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
        if (!known || user.IsMfaEnabled)
        {
            await otpService.IssueAsync(user.Id, user.PhoneNumber, OtpPurpose.NewDeviceVerification, cancellationToken);
            var mfaToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, cancellationToken);
            return new LoginResponse(true, mfaToken, DateTime.UtcNow.AddMinutes(15), null);
        }

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, cancellationToken);
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

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, cancellationToken);
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

        var accessToken = await tokenService.GenerateAccessTokenAsync(user.Id, user.Email, cancellationToken);
        return new AuthResponse(accessToken, DateTime.UtcNow.AddMinutes(15), csrfProtectionService.GenerateToken());
    }
}
