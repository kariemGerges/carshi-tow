using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.DTOs;

public sealed record RequestPhotoUploadSlotRequest(string FileName, string ContentType, long FileSizeBytes);

public sealed record RequestPhotoUploadSlotResponse(Guid PhotoId, string UploadUrl, string ObjectKey);

public sealed record ConfirmPhotoUploadRequest(
    Guid PhotoId,
    string FileName,
    int FileSizeBytes,
    string MimeType,
    short WidthPx,
    short HeightPx,
    DateTime? TakenAtUtc);

public sealed record PhotoInPackDto(
    Guid Id,
    string OriginalS3Key,
    string? PreviewS3Key,
    string FileName,
    int FileSizeBytes,
    string MimeType,
    short WidthPx,
    short HeightPx,
    PhotoCategory? AiCategory,
    bool ManualCategoryOverride,
    short SortOrder);

public sealed record UpdatePhotoInPackRequest(PhotoCategory? AiCategoryOverride, short? SortOrder);
