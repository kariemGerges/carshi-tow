using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.ValueObjects;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class DeviceRepository(AppDbContext dbContext) : IDeviceRepository
{
    public Task<Device?> GetByFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken) =>
        dbContext.Devices.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Fingerprint == new DeviceFingerprint(fingerprint),
            cancellationToken);

    public Task AddAsync(Device device, CancellationToken cancellationToken) =>
        dbContext.Devices.AddAsync(device, cancellationToken).AsTask();

    public Task UpdateAsync(Device device, CancellationToken cancellationToken)
    {
        dbContext.Devices.Update(device);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
