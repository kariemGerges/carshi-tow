namespace CarshiTow.Application.Interfaces;

/// <summary>Issues time-limited signed PUT URLs to the originals bucket (SDS §7.4 — pre-signed uploads).</summary>
public interface IOriginalsUploadSigner
{
    SignedPutObjectResult CreateSignedPut(string objectKey, string contentType, TimeSpan ttl);

    string BuildOriginalKey(Guid packId, Guid photoId, string safeFileExtensionIncludingDotLower);
}

public sealed record SignedPutObjectResult(string UploadUrl);
