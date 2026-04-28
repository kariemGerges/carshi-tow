namespace CarshiTow.Application.Interfaces;

public interface IDeviceFingerprintGenerator
{
    string Generate(string userAgent, string ipAddress, string? clientId);
}
