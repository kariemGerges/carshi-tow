using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarshiTow.Infrastructure.Services;

public sealed class PaymentNotificationService(
    AppDbContext db,
    IEmailSender email,
    ISmsSender sms,
    ILogger<PaymentNotificationService> logger) : IPaymentNotificationService
{
    public async Task NotifyPackPaidAsync(PhotoPack pack, Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            var yard = await db.TowYards
                .AsNoTracking()
                .Include(y => y.OwnerUser)
                .FirstOrDefaultAsync(y => y.Id == pack.TowYardId, cancellationToken);
            if (yard is null)
            {
                return;
            }

            var amountAud = (transaction.TotalAmountCents / 100m).ToString("F2");
            var towSubject = $"Payment received — pack {pack.VehicleRego} (${amountAud} AUD)";
            var towBody =
                $"A payment succeeded for photo pack {pack.VehicleRego} ({pack.VehicleMake} {pack.VehicleModel}).\n" +
                $"Gross: ${amountAud} AUD. Net to yard (record): ${(transaction.NetToTowYardCents / 100m):F2} AUD.\n" +
                $"Transaction id: {transaction.Id:D}.";

            if (!string.IsNullOrWhiteSpace(yard.OwnerUser.Email))
            {
                await email.SendAsync(yard.OwnerUser.Email, towSubject, towBody, cancellationToken);
            }

            var phone = string.IsNullOrWhiteSpace(yard.Phone) ? yard.OwnerUser.PhoneNumber : yard.Phone;
            if (!string.IsNullOrWhiteSpace(phone))
            {
                await sms.SendAsync(
                    phone.Trim(),
                    $"CarshiTow: payment received for {pack.VehicleRego}. ${amountAud} AUD. Tx {transaction.Id:N}.",
                    cancellationToken);
            }

            var insurerEmail = transaction.InsurerEmail.Trim();
            if (!string.IsNullOrWhiteSpace(insurerEmail) &&
                !insurerEmail.Equals("unknown@insurer.local", StringComparison.OrdinalIgnoreCase))
            {
                var invSubject = $"Tax invoice / receipt — {yard.BusinessName} — {pack.VehicleRego}";
                var invBody =
                    $"Thank you for your payment.\n\n" +
                    $"Tow yard: {yard.BusinessName}\n" +
                    $"Vehicle: {pack.VehicleYear} {pack.VehicleMake} {pack.VehicleModel} ({pack.VehicleRego})\n" +
                    $"Amount: ${amountAud} AUD (incl. GST where applicable per tow yard invoice).\n" +
                    $"Use your original payment link to download photos and PDF invoice while access is valid.\n";
                await email.SendAsync(insurerEmail, invSubject, invBody, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Payment notification failed for pack {PackId}", pack.Id);
        }
    }
}
