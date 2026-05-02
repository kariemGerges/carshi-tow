using CarshiTow.Application.Configuration;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.PhotoPacks;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CarshiTow.Application.Services;

public sealed class PackPhotoManagementService(
    IPhotoPackRepository packs,
    ITowYardPartyResolver yardResolver,
    IOriginalsUploadSigner signer,
    IAuditLogWriter audit,
    IOptions<ObjectStorageSettings> storageOptions,
    IPreviewJobQueue previewJobQueue) : IPackPhotoManagementService
{
    private readonly ObjectStorageSettings _storage = storageOptions.Value;

    public async Task<RequestPhotoUploadSlotResponse> RequestUploadSlotAsync(
        Guid actorUserId,
        Guid packId,
        RequestPhotoUploadSlotRequest request,
        CancellationToken cancellationToken)
    {
        var ctx = await yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);

        var pack = await packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        EnsurePackAllowsPhotoWrites(pack);

        var mime = NormalizeContentType(request.ContentType);
        if (!PhotoPackRules.IsAcceptedImageMime(mime))
        {
            throw new InvalidOperationException("Only JPEG, PNG, or HEIC uploads are permitted.");
        }

        if (request.FileSizeBytes is < 1 or > PhotoPackRules.MaxPhotoUploadBytes)
        {
            throw new InvalidOperationException($"File size must be between 1 byte and {PhotoPackRules.MaxPhotoUploadBytes} bytes.");
        }

        var activeCount = await packs.CountActivePhotosAsync(packId, cancellationToken);
        if (activeCount >= PhotoPackRules.MaxPhotosPerPack)
        {
            throw new InvalidOperationException($"A pack cannot contain more than {PhotoPackRules.MaxPhotosPerPack} photos.");
        }

        if (!TryResolveSafeExtension(request.FileName, mime, out var dotExtLower))
        {
            throw new InvalidOperationException("File name extension does not match the declared content type.");
        }

        var photoId = Guid.NewGuid();
        var objectKey = signer.BuildOriginalKey(pack.Id, photoId, dotExtLower);
        var ttl = TimeSpan.FromMinutes(Math.Clamp(_storage.PresignedPutExpirationMinutes, 5, 120));
        var uploadUrl = signer.CreateSignedPut(objectKey, mime, ttl).UploadUrl;

        var now = DateTime.UtcNow;
        var safeName = SanitizeUploadedFileName(request.FileName);
        var photo = new Photo
        {
            Id = photoId,
            PackId = pack.Id,
            OriginalS3Key = objectKey,
            FileName = safeName,
            FileSizeBytes = 0,
            MimeType = mime,
            WidthPx = 9,
            HeightPx = 9,
            SortOrder = (short)activeCount,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await packs.AddPhotoAsync(photo, cancellationToken);
        await SyncPhotoCountAsync(pack, cancellationToken);

        await audit.WriteAsync(
            actorUserId,
            "PHOTO_UPLOAD_SLOT",
            "photo",
            photo.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: $"{{\"packId\":\"{pack.Id:D}\"}}",
            cancellationToken);

        await packs.SaveChangesAsync(cancellationToken);

        return new RequestPhotoUploadSlotResponse(photoId, uploadUrl, objectKey);
    }

    public async Task<PhotoInPackDto> ConfirmUploadAsync(
        Guid actorUserId,
        Guid packId,
        ConfirmPhotoUploadRequest request,
        CancellationToken cancellationToken)
    {
        var ctx = await yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);
        var pack = await packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);
        EnsurePackAllowsPhotoWrites(pack);

        var photo = await packs.GetTrackedPhotoInPackAsync(packId, request.PhotoId, cancellationToken) ??
                    throw new KeyNotFoundException("Photo not found.");

        if (photo.FileSizeBytes > 0)
        {
            throw new InvalidOperationException("This photo upload was already confirmed.");
        }

        var mime = NormalizeContentType(request.MimeType);
        if (!PhotoPackRules.IsAcceptedImageMime(mime))
        {
            throw new InvalidOperationException("Only JPEG, PNG, or HEIC uploads are permitted.");
        }

        if (request.FileSizeBytes < 1 || (long)request.FileSizeBytes > PhotoPackRules.MaxPhotoUploadBytes)
        {
            throw new InvalidOperationException("Invalid confirmed file size.");
        }

        if (request.WidthPx < PhotoPackRules.MinPhotoWidthPx || request.HeightPx < PhotoPackRules.MinPhotoHeightPx)
        {
            throw new InvalidOperationException(
                $"Photos must meet the minimum resolution {PhotoPackRules.MinPhotoWidthPx}x{PhotoPackRules.MinPhotoHeightPx}px.");
        }

        photo.FileName = SanitizeUploadedFileName(request.FileName);
        photo.FileSizeBytes = request.FileSizeBytes;
        photo.MimeType = mime;
        photo.WidthPx = request.WidthPx;
        photo.HeightPx = request.HeightPx;
        photo.TakenAtUtc = request.TakenAtUtc.HasValue ? DateTime.SpecifyKind(request.TakenAtUtc.Value, DateTimeKind.Utc) : null;
        photo.UpdatedAtUtc = DateTime.UtcNow;

        if (_storage.DevMirrorOriginalAsPreview)
        {
            photo.PreviewS3Key = photo.OriginalS3Key;
            photo.ThumbnailS3Key = photo.OriginalS3Key;
        }
        else
        {
            previewJobQueue.TryEnqueue(photo.Id);
        }

        await audit.WriteAsync(
            actorUserId,
            "PHOTO_UPLOAD_CONFIRMED",
            "photo",
            photo.Id,
            ipAddress: null,
            userAgent: null,
            metadataJson: null,
            cancellationToken);

        await packs.SaveChangesAsync(cancellationToken);

        return MapPhoto(photo);
    }

    public async Task<IReadOnlyList<PhotoInPackDto>> ListPhotosAsync(
        Guid actorUserId,
        Guid packId,
        CancellationToken cancellationToken)
    {
        var ctx = await yardResolver.ResolveAsync(actorUserId, cancellationToken);

        var pack = await packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        var list = await packs.ListActivePhotosUntrackedAsync(packId, cancellationToken);
        return list.Select(MapPhoto).ToList();
    }

    public async Task<PhotoInPackDto> PatchPhotoAsync(
        Guid actorUserId,
        Guid packId,
        Guid photoId,
        UpdatePhotoInPackRequest request,
        CancellationToken cancellationToken)
    {
        var ctx = await yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);
        var pack = await packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Photos can only be edited while the pack is a draft.");
        }

        var photo = await packs.GetTrackedPhotoInPackAsync(packId, photoId, cancellationToken) ??
                    throw new KeyNotFoundException("Photo not found.");

        if (request.AiCategoryOverride is PhotoCategory cat)
        {
            photo.AiCategory = cat;
            photo.ManualCategoryOverride = true;
        }

        if (request.SortOrder is not null)
        {
            photo.SortOrder = request.SortOrder.Value;
        }

        photo.UpdatedAtUtc = DateTime.UtcNow;

        await packs.SaveChangesAsync(cancellationToken);
        return MapPhoto(photo);
    }

    public async Task DeletePhotoAsync(
        Guid actorUserId,
        Guid packId,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var ctx = await yardResolver.ResolveAsync(actorUserId, cancellationToken);
        TowYardContentGuard.EnsureYardActiveForContentWrites(ctx);
        var pack = await packs.GetTrackedPackAsync(packId, cancellationToken) ??
                   throw new KeyNotFoundException("Photo pack not found.");

        PhotoPackAccess.EnsurePackForActor(ctx, pack, actorUserId);

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Photos can only be removed while the pack is a draft.");
        }

        var photo = await packs.GetTrackedPhotoInPackAsync(packId, photoId, cancellationToken) ??
                    throw new KeyNotFoundException("Photo not found.");

        photo.DeletedAtUtc = DateTime.UtcNow;
        photo.UpdatedAtUtc = DateTime.UtcNow;

        await SyncPhotoCountAsync(pack, cancellationToken);
        await packs.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncPhotoCountAsync(PhotoPack pack, CancellationToken cancellationToken)
    {
        pack.PhotoCount = (short)await packs.CountActivePhotosAsync(pack.Id, cancellationToken);
        pack.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void EnsurePackAllowsPhotoWrites(PhotoPack pack)
    {
        if (pack.FraudFlagged)
        {
            throw new InvalidOperationException("This pack is frozen by the platform administrator.");
        }

        if (pack.Status != PhotoPackStatus.Draft)
        {
            throw new InvalidOperationException("Photos can only be changed while the pack is a draft.");
        }
    }

    private static PhotoInPackDto MapPhoto(Photo photo) =>
        new(
            photo.Id,
            photo.OriginalS3Key,
            photo.PreviewS3Key,
            photo.FileName,
            photo.FileSizeBytes,
            photo.MimeType,
            photo.WidthPx,
            photo.HeightPx,
            photo.AiCategory,
            photo.ManualCategoryOverride,
            photo.SortOrder);

    private static string NormalizeContentType(string contentType)
    {
        var m = contentType.Trim();
        var semi = m.IndexOf(';');
        if (semi >= 0)
        {
            m = m[..semi];
        }

        return m.Trim().ToLowerInvariant() switch
        {
            "image/jpg" or "image/pjpeg" => "image/jpeg",
            "image/heif" => "image/heic",
            var x => x
        };
    }

    private static string SanitizeUploadedFileName(string fileName)
    {
        var trimmed = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "upload.bin";
        }

        trimmed = trimmed.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        return trimmed[..Math.Min(trimmed.Length, 240)];
    }

    /// <returns>Dot-prefixed lower extension (.jpg|.jpeg|.png|.heic).</returns>
    private static bool TryResolveSafeExtension(string fileName, string normalisedMime, out string dotExtLower)
    {
        dotExtLower = string.Empty;
        var raw = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();
        var effective = MapExtensionAlias(raw);

        if (effective.Length is < 2 or > 10)
        {
            effective = normalisedMime switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/heic" => ".heic",
                _ => string.Empty
            };
        }

        if (effective.Length is < 2 or > 10)
        {
            return false;
        }

        var ok = (normalisedMime, effective) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/heic", ".heic" or ".heif") => true,
            _ => false
        };

        if (!ok)
        {
            return false;
        }

        dotExtLower = effective is ".heif" ? ".heic" : effective;
        return true;
    }

    private static string MapExtensionAlias(string ext) =>
        ext switch
        {
            ".jpeg" or ".jpe" or ".jfif" => ".jpg",
            ".heif" => ".heic",
            _ => ext
        };
}