namespace CarshiTow.Application.Interfaces;

/// <summary>Issues time-limited signed PUT URLs to the originals bucket (SDS §7.4 — pre-signed uploads).</summary>
public interface IOriginalsUploadSigner
{
    SignedPutObjectResult CreateSignedPut(string objectKey, string contentType, TimeSpan ttl);

    /// <summary>Time-limited GET for originals or previews (SRS §8.2 signed URLs).</summary>
    string CreateSignedGet(string objectKey, TimeSpan ttl);

    string BuildOriginalKey(Guid packId, Guid photoId, string safeFileExtensionIncludingDotLower);

    string BuildPreviewKey(Guid packId, Guid photoId);
}

public sealed record SignedPutObjectResult(string UploadUrl);
