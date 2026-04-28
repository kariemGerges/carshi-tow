using System.Security.Cryptography;
using System.Text;
using CarshiTow.Application.Interfaces;

namespace CarshiTow.Application.Security;

public sealed class DeviceFingerprintGenerator : IDeviceFingerprintGenerator
{
    public string Generate(string userAgent, string ipAddress, string? clientId)
    {
        var source = $"{userAgent}|{ipAddress}|{clientId ?? string.Empty}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(bytes);
    }
}
