namespace CarshiTow.Application.PhotoPacks;

public static class PhotoPackRules
{
    public static bool IsAcceptedImageMime(string mime)
    {
        var m = mime.Trim().ToLowerInvariant();
        return m switch
        {
            "image/jpeg" or "image/jpg" or "image/pjpeg" => true,
            "image/png" => true,
            "image/heic" or "image/heif" => true,
            _ => false
        };
    }

    public const int MinPhotosToPublish = 5;
    public const int MaxPhotosPerPack = 100;
    public const int MinTowYardPriceCents = 5500;
    public const int MaxTowYardPriceCents = 27500;
    public const int DefaultPlatformFeeCents = 5500;
    public static readonly TimeSpan LinkTimeToLive = TimeSpan.FromHours(72);
    public const int DefaultListPageSize = 20;
    public const int MaxListPageSize = 50;

    /// <summary>SRS §TY-014 minimum uploaded resolution.</summary>
    public const short MinPhotoWidthPx = 1920;

    public const short MinPhotoHeightPx = 1080;

    /// <summary>Upper bound single photo upload (~35 MB) pending platform tuning.</summary>
    public const long MaxPhotoUploadBytes = 37_748_736;
}
