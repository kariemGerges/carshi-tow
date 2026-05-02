using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

/// <summary>Best-effort insurer / tow-yard notifications after a pack is paid (SRS payment flows).</summary>
public interface IPaymentNotificationService
{
    Task NotifyPackPaidAsync(PhotoPack pack, Transaction transaction, CancellationToken cancellationToken);
}
