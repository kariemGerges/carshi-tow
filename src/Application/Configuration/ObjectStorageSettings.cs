namespace CarshiTow.Application.Configuration;

/// <summary>S3 originals bucket configuration; set <see cref="ServiceUrl"/> for LocalStack (e.g. http://localhost:4566).</summary>
public sealed class ObjectStorageSettings
{
    public const string SectionName = "ObjectStorage";

    /// <summary>Original (full-resolution) originals prefix inside bucket.</summary>
    public string OriginalsPrefix { get; set; } = "originals";

    /// <summary>Server-generated watermarked previews (SRS §8.2 separate logical bucket in AWS; same bucket allowed for MVP).</summary>
    public string PreviewsPrefix { get; set; } = "previews";

    public string BucketName { get; set; } = "carshitow-originals-local";

    public string Region { get; set; } = "ap-southeast-2";

    /// <summary>When empty, uses environment variables or LocalStack defaults when <see cref="ServiceUrl"/> is set.</summary>
    public string AwsAccessKey { get; set; } = "";

    public string AwsSecretAccessKey { get; set; } = "";

    /// <summary>When set (LocalStack/Docker), client uses path-style addressing against this endpoint.</summary>
    public Uri? ServiceUrl { get; set; }

    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Short-lived PUT credentials for uploads.</summary>
    public int PresignedPutExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Development only: mirrors <see cref="OriginalsPrefix"/> key into preview key so publish watermark checks succeed without workers.
    /// Must be false outside dev/staging harnesses (SRS watermark must be applied server-side in production).
    /// </summary>
    public bool DevMirrorOriginalAsPreview { get; set; }
}
