namespace CarshiTow.Application.Interfaces;

public interface IJwtAccessTokenValidator
{
    bool TryGetUserId(string accessToken, out Guid userId);
}
