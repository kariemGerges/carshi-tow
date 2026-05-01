namespace CarshiTow.Application.Interfaces;

/// <summary>Issues short-lived preview URLs (SDS §8.2 — never expose raw S3 paths publicly).</summary>
public interface IFilePreviewUrlIssuer
{
    string IssuePreviewGetUrl(string storageKey, TimeSpan ttl);
}
