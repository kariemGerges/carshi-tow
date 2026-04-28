using System.Security.Cryptography;
using CarshiTow.Application.Interfaces;

namespace CarshiTow.Application.Security;

public sealed class CsrfProtectionService : ICsrfProtectionService
{
    public string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes);
    }

    public bool IsValid(string expectedToken, string providedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken) || string.IsNullOrWhiteSpace(providedToken))
        {
            return false;
        }

        try
        {
            var expectedBytes = SHA256.HashData(Convert.FromBase64String(expectedToken));
            var providedBytes = SHA256.HashData(Convert.FromBase64String(providedToken));
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
