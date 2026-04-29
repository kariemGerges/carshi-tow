using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, UserRole role, CancellationToken cancellationToken);
}
