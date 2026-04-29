using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Services;

public sealed class TokenService(IJwtTokenGenerator jwtTokenGenerator) : ITokenService
{
    public Task<string> GenerateAccessTokenAsync(Guid userId, string email, UserRole role, CancellationToken cancellationToken)
    {
        var token = jwtTokenGenerator.Generate(userId, email, role);
        return Task.FromResult(token);
    }
}
