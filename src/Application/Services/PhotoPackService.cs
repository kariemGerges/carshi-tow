using CarshiTow.Application.Configuration;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.PhotoPacks;
using CarshiTow.Application.Security;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CarshiTow.Application.Services;

public sealed class PhotoPackService : IPhotoPackService
{
    private readonly IPhotoPackRepository _packs;
    private readonly ITowYardPartyResolver _yardResolver;
    private readonly IAuditLogWriter _audit;
    private readonly PublicLinksSettings _publicLinks;

    public PhotoPackService(
        IPhotoPackRepository packs,
        ITowYardPartyResolver yardResolver,
        IAuditLogWriter audit,
        IOptions<PublicLinksSettings> publicLinksOptions)
    {
        _packs = packs;
        _yardResolver = yardResolver;
        _audit = audit;
        _publicLinks = publicLinksOptions.Value;
    }

    public async Task<PhotoPackDto> CreateDraftAsync(
        Guid actorUserId,
        CreatePhotoPackDraftRequest request,
        CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);

        var now = DateTime.UtcNow;
        var platformFee = ctx.PlatformFeeCents;
        var pack = new PhotoPack
        {
            Id = Guid.NewGuid(),
            TowYardId = ctx.TowYardId,
            CreatedByUserId = actorUserId,
            VehicleRego = request.VehicleRego.Trim().ToUpperInvariant(),
            VehicleRegoState = request.VehicleRegoState,
            VehicleMake = request.VehicleMake.Trim(),
            VehicleModel = request.VehicleModel.Trim(),
            VehicleYear = request.VehicleYear,
            VehicleVin = string.IsNullOrWhiteSpace(request.VehicleVin) ? null : request.VehicleVin.Trim(),
            ClaimReference = string.IsNullOrWhiteSpace(request.ClaimReference) ? null : request.ClaimReference.Trim(),
            TowYardReference =
                string.IsNullOrWhiteSpace(request.TowYardReference) ? null : request.TowYardReference.Trim(),
            Status = PhotoPackStatus.Draft,
            PhotoCount = 0,
            TowYardPriceCents = request.TowYardPriceCents,
            PlatformFeeCents = platformFee,
            TotalPriceCents = request.TowYardPriceCents + platformFee,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _packs.AddAsync(pack, cancellationToken);
        await _audit.WriteAsync(
            actorUserId,
            "PACK_CREATED",
            "photo_pack",
            pack.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: $"{{\"towYardId\":\"{pack.TowYardId:D}\"}}",
            cancellationToken);
        await _packs.SaveChangesAsync(cancellationToken);

        return Map(pack);
    }

    public async Task<PhotoPackDto> UpdateDraftAsync(
        Guid actorUserId,
        Guid packId,
        UpdatePhotoPackDraftRequest request,
        CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);
        var pack = await _packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Only draft packs can be updated.");
        }

        if (request.VehicleRego is not null)
        {
            pack.VehicleRego = request.VehicleRego.Trim().ToUpperInvariant();
        }

        if (request.VehicleRegoState is not null)
        {
            pack.VehicleRegoState = request.VehicleRegoState.Value;
        }

        if (request.VehicleMake is not null)
        {
            pack.VehicleMake = request.VehicleMake.Trim();
        }

        if (request.VehicleModel is not null)
        {
            pack.VehicleModel = request.VehicleModel.Trim();
        }

        if (request.VehicleYear is not null)
        {
            pack.VehicleYear = request.VehicleYear.Value;
        }

        if (request.VehicleVin is not null)
        {
            pack.VehicleVin = string.IsNullOrWhiteSpace(request.VehicleVin) ? null : request.VehicleVin.Trim();
        }

        if (request.ClaimReference is not null)
        {
            pack.ClaimReference = string.IsNullOrWhiteSpace(request.ClaimReference)
                ? null
                : request.ClaimReference.Trim();
        }

        if (request.TowYardReference is not null)
        {
            pack.TowYardReference = string.IsNullOrWhiteSpace(request.TowYardReference)
                ? null
                : request.TowYardReference.Trim();
        }

        if (request.TowYardPriceCents is not null)
        {
            pack.TowYardPriceCents = request.TowYardPriceCents.Value;
            pack.PlatformFeeCents = ctx.PlatformFeeCents;
            pack.TotalPriceCents = pack.TowYardPriceCents + pack.PlatformFeeCents;
        }

        pack.UpdatedAtUtc = DateTime.UtcNow;
        await _audit.WriteAsync(
            actorUserId,
            "PACK_UPDATED",
            "photo_pack",
            pack.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: null,
            cancellationToken);
        await _packs.SaveChangesAsync(cancellationToken);

        return Map(pack);
    }

    public async Task DeleteDraftAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);
        var pack = await _packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Only draft packs can be deleted.");
        }

        pack.DeletedAtUtc = DateTime.UtcNow;
        pack.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            actorUserId,
            "PACK_DELETED",
            "photo_pack",
            pack.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: null,
            cancellationToken);
        await _packs.SaveChangesAsync(cancellationToken);
    }

    public async Task<PhotoPackPublishedResponse> PublishAsync(
        Guid actorUserId,
        Guid packId,
        CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);

        if (ctx.YardStatus != TowYardStatus.Active)
        {
            throw new InvalidOperationException("Tow yard must be active before packs can go live.");
        }

        var pack = await _packs.GetTrackedPackWithPhotosAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.FraudFlagged)
        {
            throw new InvalidOperationException("This pack is frozen by the platform administrator.");
        }

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Only draft packs can be published.");
        }

        var photos = ActivePhotos(pack).ToList();
        if (photos.Count is < PhotoPackRules.MinPhotosToPublish or > PhotoPackRules.MaxPhotosPerPack)
        {
            throw new InvalidOperationException(
                $"Publishing requires between {PhotoPackRules.MinPhotosToPublish} and {PhotoPackRules.MaxPhotosPerPack} photos.");
        }

        foreach (var p in photos)
        {
            if (p.FileSizeBytes <= 0)
            {
                throw new InvalidOperationException("Each photo must be fully uploaded before publishing.");
            }

            if (!PhotoPackRules.IsAcceptedImageMime(p.MimeType))
            {
                throw new InvalidOperationException("Each photo must be JPEG, PNG, or HEIC before publishing.");
            }

            if (p.WidthPx < PhotoPackRules.MinPhotoWidthPx || p.HeightPx < PhotoPackRules.MinPhotoHeightPx)
            {
                throw new InvalidOperationException(
                    $"Each photo must meet the minimum resolution of {PhotoPackRules.MinPhotoWidthPx}x{PhotoPackRules.MinPhotoHeightPx}px (SRS §TY-014).");
            }

            if (string.IsNullOrWhiteSpace(p.PreviewS3Key))
            {
                throw new InvalidOperationException(
                    "Every photo must have a generated watermarked preview before publishing (SRS §IN-003).");
            }
        }

        var raw = SecureLinkTokens.CreateRawUrlSegment();
        var hash = SecureLinkTokens.HashForStorage(raw);
        var expires = DateTime.UtcNow.Add(PhotoPackRules.LinkTimeToLive);

        pack.LinkToken = hash;
        pack.LinkExpiresAtUtc = expires;
        pack.Status = PhotoPackStatus.Active;
        pack.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            actorUserId,
            "PACK_PUBLISHED",
            "photo_pack",
            pack.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: $"{{\"linkExpiresAtUtc\":\"{expires:o}\"}}",
            cancellationToken);
        await _packs.SaveChangesAsync(cancellationToken);

        var shareUrl = $"{_publicLinks.InsurerAccessBaseUrl.TrimEnd('/')}/{raw}";
        return new PhotoPackPublishedResponse(Map(pack), shareUrl, expires);
    }

    public async Task<PhotoPackPublishedResponse> RegenerateLinkAsync(
        Guid actorUserId,
        Guid packId,
        CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);

        if (ctx.YardStatus != TowYardStatus.Active)
        {
            throw new InvalidOperationException("Tow yard must be active to regenerate insurer links.");
        }

        var pack = await _packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.FraudFlagged)
        {
            throw new InvalidOperationException("This pack is frozen by the platform administrator.");
        }

        if (pack.Status is PhotoPackStatus.Paid || pack.PaidAtUtc is not null)
        {
            throw new InvalidOperationException("Paid packs cannot regenerate public links.");
        }

        if (pack.Status is not (PhotoPackStatus.Active or PhotoPackStatus.Expired))
        {
            throw new InvalidOperationException("Links can only be regenerated for active or unpaid expired packs.");
        }

        TryMarkExpiredUnpaidSync(pack, DateTime.UtcNow);

        var raw = SecureLinkTokens.CreateRawUrlSegment();
        var hash = SecureLinkTokens.HashForStorage(raw);
        var expires = DateTime.UtcNow.Add(PhotoPackRules.LinkTimeToLive);

        pack.LinkToken = hash;
        pack.LinkExpiresAtUtc = expires;
        pack.Status = PhotoPackStatus.Active;
        pack.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            actorUserId,
            "LINK_REGENERATED",
            "photo_pack",
            pack.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: $"{{\"linkExpiresAtUtc\":\"{expires:o}\"}}",
            cancellationToken);
        await _packs.SaveChangesAsync(cancellationToken);

        var shareUrl = $"{_publicLinks.InsurerAccessBaseUrl.TrimEnd('/')}/{raw}";
        return new PhotoPackPublishedResponse(Map(pack), shareUrl, expires);
    }

    public async Task<PhotoPackDto> GetByIdAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);

        await _packs.MarkExpiredUnpaidPacksForYardAsync(ctx.TowYardId, DateTime.UtcNow, cancellationToken);

        var pack = await _packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        TryMarkExpiredUnpaidSync(pack, DateTime.UtcNow);
        await _packs.SaveChangesAsync(cancellationToken);

        return Map(pack);
    }

    public async Task<(IReadOnlyList<PhotoPackDto> Items, string? NextCursor)> ListAsync(
        Guid actorUserId,
        PhotoPackListQuery query,
        CancellationToken cancellationToken)
    {
        var ctx = await _yardResolver.ResolveAsync(actorUserId, cancellationToken);

        await _packs.MarkExpiredUnpaidPacksForYardAsync(ctx.TowYardId, DateTime.UtcNow, cancellationToken);

        var take = Math.Clamp(query.PageSize, 1, PhotoPackRules.MaxListPageSize);

        var criteria = new PhotoPackListCriteria(
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc,
            query.MinTowYardPriceCents,
            query.MaxTowYardPriceCents,
            CreatedByUserId: ctx.Visibility == TowYardVisibilityScope.AssignedPacksOnly ? actorUserId : null);

        var (items, next) = await _packs.ListAsync(ctx.TowYardId, criteria, take, query.Cursor, cancellationToken);

        return (items.Select(Map).ToList(), next);
    }

    public async Task<PhotoPackStatsDto> GetStatsAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken)
    {
        var dto = await GetByIdAsync(actorUserId, packId, cancellationToken);
        var isPaid = dto.Status == PhotoPackStatus.Paid || dto.PaidAtUtc is not null;
        return new PhotoPackStatsDto(
            dto.Id,
            dto.LinkViewCount,
            isPaid,
            dto.PaidAtUtc,
            dto.Status,
            dto.LinkExpiresAtUtc);
    }

    private static IEnumerable<Photo> ActivePhotos(PhotoPack pack) =>
        pack.Photos.Where(p => p.DeletedAtUtc is null);

    private static void TryMarkExpiredUnpaidSync(PhotoPack pack, DateTime utcNow)
    {
        if (pack.Status == PhotoPackStatus.Active && pack.PaidAtUtc is null && pack.LinkExpiresAtUtc is not null &&
            pack.LinkExpiresAtUtc.Value < utcNow)
        {
            pack.Status = PhotoPackStatus.Expired;
            pack.UpdatedAtUtc = utcNow;
        }
    }

    private static PhotoPackDto Map(PhotoPack p) =>
        new(
            p.Id,
            p.TowYardId,
            p.CreatedByUserId,
            p.VehicleRego,
            p.VehicleRegoState,
            p.VehicleMake,
            p.VehicleModel,
            p.VehicleYear,
            p.VehicleVin,
            p.ClaimReference,
            p.TowYardReference,
            p.Status,
            p.PhotoCount,
            p.QualityScore,
            p.DamageSeverity,
            p.TowYardPriceCents,
            p.PlatformFeeCents,
            p.TotalPriceCents,
            p.LinkExpiresAtUtc,
            p.LinkViewCount,
            p.PaidAtUtc,
            p.FraudFlagged,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);
}
