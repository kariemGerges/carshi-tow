namespace CarshiTow.Application.Interfaces;

public interface ICookieManager
{
    void SetRefreshToken(string refreshToken);
    string? GetRefreshToken();
    void ClearRefreshToken();
}
