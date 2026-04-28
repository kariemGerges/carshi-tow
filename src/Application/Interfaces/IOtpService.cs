using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public interface IOtpService
{
    Task IssueAsync(Guid userId, string phoneNumber, OtpPurpose purpose, CancellationToken cancellationToken);
    Task<bool> VerifyAsync(Guid userId, string code, OtpPurpose purpose, CancellationToken cancellationToken);
}
