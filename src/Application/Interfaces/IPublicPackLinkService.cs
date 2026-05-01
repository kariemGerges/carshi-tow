using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

public interface IPublicPackLinkService
{
    /// <summary>Resolve public link by raw token; optionally increments view count (SRS §2.2.3 TY-035).</summary>
    Task<PackLinkPreviewDto?> GetPreviewAsync(string rawToken, bool incrementViewCount, CancellationToken cancellationToken);
}
