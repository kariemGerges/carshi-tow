using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class Photo
{
    public Guid Id { get; set; }
    public Guid PackId { get; set; }
    public string OriginalS3Key { get; set; } = string.Empty;
    public string? PreviewS3Key { get; set; }
    public string? ThumbnailS3Key { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public short WidthPx { get; set; }
    public short HeightPx { get; set; }
    public PhotoCategory? AiCategory { get; set; }
    public bool ManualCategoryOverride { get; set; }
    public short? QualityScore { get; set; }
    public short SortOrder { get; set; }
    public DateTime? TakenAtUtc { get; set; }
    public decimal? GpsLat { get; set; }
    public decimal? GpsLng { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public PhotoPack Pack { get; set; } = default!;
}
