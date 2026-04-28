namespace CarshiTow.Application.Interfaces;

public interface ICsrfProtectionService
{
    string GenerateToken();
    bool IsValid(string expectedToken, string providedToken);
}
