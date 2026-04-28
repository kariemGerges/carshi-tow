using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public interface IOtpCodeRepository
{
    Task AddAsync(OtpCode code, CancellationToken cancellationToken);
    Task<OtpCode?> GetLatestActiveAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken);
    Task UpdateAsync(OtpCode code, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
