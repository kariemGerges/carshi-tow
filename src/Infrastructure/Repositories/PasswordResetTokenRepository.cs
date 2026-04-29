using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository(AppDbContext dbContext) : IPasswordResetTokenRepository
{
    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow,
            cancellationToken);

    public Task InvalidateActiveForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens
            .Where(x => x.UserId == userId && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, _ => DateTime.UtcNow), cancellationToken);

    public Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        dbContext.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
