using System.Security.Cryptography;
using System.Text;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Services;

public sealed class RefreshTokenService(IRefreshTokenRepository refreshTokenRepository) : IRefreshTokenService
{
    public async Task<(RefreshToken Token, string RawToken)> CreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokenRepository.AddAsync(token, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return (token, raw);
    }

    public Task<RefreshToken?> ValidateAsync(string rawToken, CancellationToken cancellationToken) =>
        refreshTokenRepository.GetByHashAsync(Hash(rawToken), cancellationToken);

    public async Task<(RefreshToken Token, string RawToken)> RotateAsync(RefreshToken current, CancellationToken cancellationToken)
    {
        current.RevokedAtUtc = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(current, cancellationToken);

        var replacement = await CreateAsync(current.UserId, cancellationToken);
        current.ReplacedByTokenHash = replacement.Token.TokenHash;
        await refreshTokenRepository.UpdateAsync(current, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return replacement;
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        token.RevokedAtUtc = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(token, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
