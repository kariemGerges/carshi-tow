using CarshiTow.Application.Abn;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure.External;

/// <summary>Local checksum validation; optional REST hook when configured (SRS TY-002 future live ABR).</summary>
public sealed class AbnRegistryClient(IOptions<AbnLookupSettings> options, ILogger<AbnRegistryClient> logger)
    : IAbnRegistryClient
{
    public Task<string> RequireValidCanonicalAbnAsync(string abnInput, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var settings = options.Value;

        if (!AbnChecksum.TryValidate(abnInput, out var normalized))
        {
            throw new InvalidOperationException("ABN format or checksum validation failed.");
        }

        if (settings.UseJsonLookup)
        {
            if (settings.JsonLookupBaseUri is null)
            {
                logger.LogWarning(
                    "AbnLookup:UseJsonLookup is enabled but JsonLookupBaseUri is not set; deferring remote validation.");
            }

            logger.LogWarning("Remote ABN JSON verification is enabled but not yet wired — checksum validation only.");
        }

        return Task.FromResult(normalized);
    }
}
