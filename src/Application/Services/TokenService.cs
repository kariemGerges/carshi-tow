using CarshiTow.Application.Interfaces;

namespace CarshiTow.Application.Services;

public sealed class TokenService(IJwtTokenGenerator jwtTokenGenerator) : ITokenService
{
    public Task<string> GenerateAccessTokenAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var token = jwtTokenGenerator.Generate(userId, email);
        return Task.FromResult(token);
    }
}
