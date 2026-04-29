using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task InvalidateActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
