using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.Security;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Services;

public sealed class PublicPackLinkService(
    IPhotoPackRepository packs,
    IFilePreviewUrlIssuer previewIssuer,
    AppDbContext db) : IPublicPackLinkService
{
    public async Task<PackLinkPreviewDto?> GetPreviewAsync(
        string rawToken,
        bool incrementViewCount,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = SecureLinkTokens.HashForStorage(rawToken.Trim());
        var pack = await packs.GetTrackedByLinkHashAsync(hash, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        if (pack.FraudFlagged)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        ApplyExpirySync(pack, utcNow);
        await packs.SaveChangesAsync(cancellationToken);

        var isPaid = pack.Status == PhotoPackStatus.Paid || pack.PaidAtUtc.HasValue;

        var linkExpired = pack.LinkExpiresAtUtc is not null &&
            pack.LinkExpiresAtUtc.Value < utcNow &&
            !isPaid;

        if (incrementViewCount && !pack.FraudFlagged)
        {
            await db.PhotoPacks
                .Where(p => p.Id == pack.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LinkViewCount, p => p.LinkViewCount + 1), cancellationToken);
        }

        var suppressPreviews = linkExpired || isPaid;
        var previews = BuildPreviewPhotos(pack, suppressPreviews);

        return new PackLinkPreviewDto(
            pack.Id,
            pack.VehicleRego,
            pack.VehicleRegoState,
            pack.VehicleMake,
            pack.VehicleModel,
            pack.VehicleYear,
            pack.DamageSeverity,
            PackPhotoCount(pack),
            pack.TotalPriceCents,
            pack.TowYardPriceCents,
            pack.PlatformFeeCents,
            pack.Status,
            linkExpired,
            isPaid,
            pack.LinkExpiresAtUtc,
            previews);
    }

    private static short PackPhotoCount(PhotoPack pack) =>
        (short)pack.Photos.Count(p => p.DeletedAtUtc is null);

    private List<PackPreviewPhotoDto> BuildPreviewPhotos(PhotoPack pack, bool suppressPreviews)
    {
        if (suppressPreviews)
        {
            return [];
        }

        var ttl = TimeSpan.FromHours(1);
        List<PackPreviewPhotoDto> list = [];
        foreach (var ph in pack.Photos.Where(p => p.DeletedAtUtc is null).OrderBy(p => p.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(ph.PreviewS3Key))
            {
                continue;
            }

            list.Add(new PackPreviewPhotoDto(
                ph.Id,
                previewIssuer.IssuePreviewGetUrl(ph.PreviewS3Key!, ttl),
                ph.SortOrder));
        }

        return list;
    }

    private static void ApplyExpirySync(PhotoPack pack, DateTime utcNow)
    {
        if (pack.Status == PhotoPackStatus.Active && pack.PaidAtUtc is null && pack.LinkExpiresAtUtc is not null &&
            pack.LinkExpiresAtUtc.Value < utcNow)
        {
            pack.Status = PhotoPackStatus.Expired;
            pack.UpdatedAtUtc = utcNow;
        }
    }
}
