using System.Security.Cryptography;
using System.Text;

namespace CarshiTow.Application.Security;

/// <summary>
/// Cryptographic link tokens: raw segment appears only in shared URLs; only SHA-256 hex (64 chars) is stored (SDS §8.2).
/// </summary>
public static class SecureLinkTokens
{
    public static string CreateRawUrlSegment()
    {
        Span<byte> buf = stackalloc byte[32];
        RandomNumberGenerator.Fill(buf);
        return ToBase64Url(buf);
    }

    public static string HashForStorage(string rawSegment)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSegment));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ToBase64Url(ReadOnlySpan<byte> data)
    {
        var s = Convert.ToBase64String(data);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
