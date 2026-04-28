using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken);
    Task AddAsync(Device device, CancellationToken cancellationToken);
    Task UpdateAsync(Device device, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
