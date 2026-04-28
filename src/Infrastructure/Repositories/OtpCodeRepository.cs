using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class OtpCodeRepository(AppDbContext dbContext) : IOtpCodeRepository
{
    public Task AddAsync(OtpCode code, CancellationToken cancellationToken) =>
        dbContext.OtpCodes.AddAsync(code, cancellationToken).AsTask();

    public Task<OtpCode?> GetLatestActiveAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken) =>
        dbContext.OtpCodes
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task UpdateAsync(OtpCode code, CancellationToken cancellationToken)
    {
        dbContext.OtpCodes.Update(code);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
