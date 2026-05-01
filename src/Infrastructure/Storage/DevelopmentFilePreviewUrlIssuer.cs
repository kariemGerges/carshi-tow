using CarshiTow.Application.Interfaces;

namespace CarshiTow.Infrastructure.Storage;

/// <summary>
/// Stub issuer for local/dev; replace with real CloudFront signed URLs or S3 pre-signed URLs in production (SDS §8.2).
/// </summary>
public sealed class DevelopmentFilePreviewUrlIssuer : IFilePreviewUrlIssuer
{
    public string IssuePreviewGetUrl(string storageKey, TimeSpan ttl)
    {
        _ = ttl;
        var key = Uri.EscapeDataString(storageKey);
        return $"https://preview.local.invalid/v1/object?key={key}";
    }
}
