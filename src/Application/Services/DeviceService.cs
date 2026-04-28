using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.ValueObjects;

namespace CarshiTow.Application.Services;

public sealed class DeviceService(IDeviceRepository deviceRepository) : IDeviceService
{
    public async Task<bool> IsKnownTrustedDeviceAsync(Guid userId, string fingerprint, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByFingerprintAsync(userId, fingerprint, cancellationToken);
        return device is not null && device.IsTrusted;
    }

    public async Task UpsertDeviceAsync(Guid userId, string fingerprint, string userAgent, string ipAddress, bool trust, CancellationToken cancellationToken)
    {
        var existing = await deviceRepository.GetByFingerprintAsync(userId, fingerprint, cancellationToken);
        if (existing is null)
        {
            var device = new Device
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Fingerprint = new DeviceFingerprint(fingerprint),
                UserAgent = userAgent,
                IpAddress = ipAddress,
                IsTrusted = trust,
                LastSeenAtUtc = DateTime.UtcNow
            };
            await deviceRepository.AddAsync(device, cancellationToken);
        }
        else
        {
            existing.LastSeenAtUtc = DateTime.UtcNow;
            existing.UserAgent = userAgent;
            existing.IpAddress = ipAddress;
            existing.IsTrusted = trust || existing.IsTrusted;
            await deviceRepository.UpdateAsync(existing, cancellationToken);
        }

        await deviceRepository.SaveChangesAsync(cancellationToken);
    }
}
