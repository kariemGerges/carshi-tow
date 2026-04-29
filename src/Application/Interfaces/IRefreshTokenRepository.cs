using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken);
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
