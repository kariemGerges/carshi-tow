using BCrypt.Net;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Services;

public sealed class OtpService(IOtpCodeRepository otpCodeRepository, ISmsSender smsSender) : IOtpService
{
    public async Task IssueAsync(Guid userId, string phoneNumber, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var entity = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            Purpose = purpose,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        await otpCodeRepository.AddAsync(entity, cancellationToken);
        await otpCodeRepository.SaveChangesAsync(cancellationToken);
        await smsSender.SendAsync(phoneNumber, $"Your CarshiTow OTP code is: {code}", cancellationToken);
    }

    public async Task<bool> VerifyAsync(Guid userId, string code, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var entity = await otpCodeRepository.GetLatestActiveAsync(userId, purpose, cancellationToken);
        if (entity is null || entity.IsExpired || entity.IsConsumed)
        {
            return false;
        }

        var valid = BCrypt.Net.BCrypt.Verify(code, entity.CodeHash);
        if (!valid)
        {
            return false;
        }

        entity.ConsumedAtUtc = DateTime.UtcNow;
        await otpCodeRepository.UpdateAsync(entity, cancellationToken);
        await otpCodeRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
