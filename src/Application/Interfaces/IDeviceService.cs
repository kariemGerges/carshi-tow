namespace CarshiTow.Application.Interfaces;

public interface IDeviceService
{
    Task<bool> IsKnownTrustedDeviceAsync(Guid userId, string fingerprint, CancellationToken cancellationToken);
    Task UpsertDeviceAsync(Guid userId, string fingerprint, string userAgent, string ipAddress, bool trust, CancellationToken cancellationToken);
}
