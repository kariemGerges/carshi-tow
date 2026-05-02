using CarshiTow.Application.Configuration;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.Security;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Billing;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace CarshiTow.Infrastructure.Services;

public sealed class LinkPaymentService(
    AppDbContext db,
    IPhotoPackRepository packs,
    IOriginalsUploadSigner signer,
    IOptions<StripeSettings> stripeOptions,
    IPaymentNotificationService paymentNotifications) : ILinkPaymentService
{
    private static readonly TimeSpan DownloadUrlTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan DownloadWindow = TimeSpan.FromDays(7);

    public async Task<CreateLinkPaymentIntentResponse?> CreatePaymentIntentAsync(string rawLinkToken, CancellationToken cancellationToken)
    {
        var pack = await ResolvePackForPaymentAsync(rawLinkToken, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var secret = stripeOptions.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("StripePayments:SecretKey is not configured.");
        }

        var options = new PaymentIntentCreateOptions
        {
            Amount = pack.TotalPriceCents,
            Currency = "aud",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string>
            {
                ["photo_pack_id"] = pack.Id.ToString("D"),
                ["tow_yard_id"] = pack.TowYardId.ToString("D"),
            },
        };

        var service = new PaymentIntentService(new StripeClient(secret));
        var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

        pack.StripePaymentIntentId = intent.Id;
        pack.UpdatedAtUtc = DateTime.UtcNow;
        await packs.SaveChangesAsync(cancellationToken);

        return new CreateLinkPaymentIntentResponse(intent.ClientSecret ?? string.Empty, intent.Id, pack.TotalPriceCents, "aud");
    }

    public async Task<ConfirmLinkPaymentResponse?> ConfirmPaymentAsync(
        string rawLinkToken,
        ConfirmLinkPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var pack = await ResolvePackForPaymentAsync(rawLinkToken, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        if (!string.Equals(pack.StripePaymentIntentId, request.PaymentIntentId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Payment intent does not match this pack.");
        }

        if (await db.Transactions.AnyAsync(
                t => t.StripePaymentIntentId == request.PaymentIntentId && t.DeletedAtUtc == null,
                cancellationToken))
        {
            var idempotentPack = await packs.GetTrackedPackWithPhotosAsync(pack.Id, cancellationToken) ?? pack;
            return new ConfirmLinkPaymentResponse(
                BuildDownloadDtos(idempotentPack, DateTime.UtcNow),
                pack.PaidAtUtc?.Add(DownloadWindow) ?? DateTime.UtcNow.Add(DownloadWindow));
        }

        var secret = stripeOptions.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("StripePayments:SecretKey is not configured.");
        }

        var service = new PaymentIntentService(new StripeClient(secret));
        var intent = await service.GetAsync(request.PaymentIntentId, cancellationToken: cancellationToken);
        if (intent.Status != "succeeded")
        {
            throw new InvalidOperationException($"Payment intent status is {intent.Status}; expected succeeded.");
        }

        var utc = DateTime.UtcNow;
        pack.Status = PhotoPackStatus.Paid;
        pack.PaidAtUtc = utc;
        pack.PaidByEmail = string.IsNullOrWhiteSpace(request.InsurerEmail) ? "unknown@insurer.local" : request.InsurerEmail.Trim();
        pack.UpdatedAtUtc = utc;

        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            PackId = pack.Id,
            TowYardId = pack.TowYardId,
            StripePaymentIntentId = intent.Id,
            StripeChargeId = intent.LatestChargeId,
            TotalAmountCents = pack.TotalPriceCents,
            PlatformFeeCents = pack.PlatformFeeCents,
            TowYardAmountCents = pack.TowYardPriceCents,
            StripeFeeCents = 0,
            NetToTowYardCents = pack.TowYardPriceCents,
            InsurerEmail = pack.PaidByEmail,
            InsurerOrgName = null,
            Status = TransactionStatus.Succeeded,
            CreatedAtUtc = utc,
            UpdatedAtUtc = utc,
        };

        db.Transactions.Add(tx);
        await packs.SaveChangesAsync(cancellationToken);

        var paidPack = await packs.GetTrackedPackWithPhotosAsync(pack.Id, cancellationToken) ?? pack;
        await paymentNotifications.NotifyPackPaidAsync(paidPack, tx, cancellationToken);

        var downloads = BuildDownloadDtos(paidPack, utc);
        return new ConfirmLinkPaymentResponse(downloads, utc.Add(DownloadWindow));
    }

    public async Task<IReadOnlyList<PackDownloadPhotoDto>?> GetPaidDownloadsAsync(
        string rawLinkToken,
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        var pack = await ResolvePackByTokenAsync(rawLinkToken, cancellationToken);
        if (pack is null || pack.PaidAtUtc is null || pack.Status != PhotoPackStatus.Paid)
        {
            return null;
        }

        if (!string.Equals(pack.StripePaymentIntentId, paymentIntentId, StringComparison.Ordinal))
        {
            return null;
        }

        if (DateTime.UtcNow > pack.PaidAtUtc.Value.Add(DownloadWindow))
        {
            throw new InvalidOperationException("Download access has expired (7 days after payment).");
        }

        pack = await packs.GetTrackedPackWithPhotosAsync(pack.Id, cancellationToken) ?? pack;
        return BuildDownloadDtos(pack, DateTime.UtcNow);
    }

    public async Task<LinkInvoicePdfResponse?> GetInvoicePdfAsync(string rawLinkToken, string paymentIntentId, CancellationToken cancellationToken)
    {
        var pack = await ResolvePackByTokenAsync(rawLinkToken, cancellationToken);
        if (pack is null || pack.PaidAtUtc is null)
        {
            return null;
        }

        if (!string.Equals(pack.StripePaymentIntentId, paymentIntentId, StringComparison.Ordinal))
        {
            return null;
        }

        var tx = await db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntentId && t.DeletedAtUtc == null, cancellationToken);
        if (tx is null)
        {
            return null;
        }

        var pdf = InvoicePdfBuilder.BuildSimpleGstInvoice(tx, pack);
        return new LinkInvoicePdfResponse(pdf, $"invoice-{tx.Id:N}.pdf");
    }

    private List<PackDownloadPhotoDto> BuildDownloadDtos(PhotoPack pack, DateTime utcNow)
    {
        var expiresAt = utcNow.Add(DownloadUrlTtl);
        var list = new List<PackDownloadPhotoDto>();
        foreach (var ph in pack.Photos.Where(p => p.DeletedAtUtc is null).OrderBy(p => p.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(ph.OriginalS3Key))
            {
                continue;
            }

            list.Add(new PackDownloadPhotoDto(
                ph.Id,
                ph.FileName,
                signer.CreateSignedGet(ph.OriginalS3Key, DownloadUrlTtl),
                expiresAt));
        }

        return list;
    }

    private async Task<PhotoPack?> ResolvePackByTokenAsync(string rawLinkToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawLinkToken))
        {
            return null;
        }

        var hash = SecureLinkTokens.HashForStorage(rawLinkToken.Trim());
        return await packs.GetTrackedByLinkHashAsync(hash, cancellationToken);
    }

    private async Task<PhotoPack?> ResolvePackForPaymentAsync(string rawLinkToken, CancellationToken cancellationToken)
    {
        var pack = await ResolvePackByTokenAsync(rawLinkToken, cancellationToken);
        if (pack is null || pack.FraudFlagged)
        {
            return null;
        }

        var utc = DateTime.UtcNow;
        if (pack.Status == PhotoPackStatus.Paid || pack.PaidAtUtc is not null)
        {
            return null;
        }

        if (pack.LinkExpiresAtUtc is not null && pack.LinkExpiresAtUtc < utc)
        {
            return null;
        }

        if (pack.Status is not (PhotoPackStatus.Active or PhotoPackStatus.Expired))
        {
            return null;
        }

        return await packs.GetTrackedPackWithPhotosAsync(pack.Id, cancellationToken);
    }
}
