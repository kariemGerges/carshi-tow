using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<(RefreshToken Token, string RawToken)> CreateAsync(Guid userId, CancellationToken cancellationToken);
    Task<RefreshToken?> ValidateAsync(string rawToken, CancellationToken cancellationToken);
    Task<(RefreshToken Token, string RawToken)> RotateAsync(RefreshToken current, CancellationToken cancellationToken);
    Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken);
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
}
