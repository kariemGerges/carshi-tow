using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, string fingerprint, string ipAddress, CancellationToken cancellationToken);
    Task<RefreshAuthResult> RefreshAsync(RefreshTokenRequest request, string rawRefreshToken, string expectedCsrfToken, CancellationToken cancellationToken);
    Task<AuthResponse> VerifyOtpAsync(Guid userId, VerifyOtpRequest request, string fingerprint, string userAgent, string ipAddress, CancellationToken cancellationToken);
}
