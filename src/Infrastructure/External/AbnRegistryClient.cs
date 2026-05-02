using System.Text.Json;
using CarshiTow.Application.Abn;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure.External;

/// <summary>SRS §TY-002 — checksum plus optional ABR JSON lookup (https://abr.business.gov.au/json).</summary>
public sealed class AbnRegistryClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AbnLookupSettings> options,
    ILogger<AbnRegistryClient> logger) : IAbnRegistryClient
{
    private const string AbrHttpClientName = "abr";

    public async Task<string> RequireValidCanonicalAbnAsync(string abnInput, CancellationToken cancellationToken)
    {
        if (!AbnChecksum.TryValidate(abnInput, out var normalized))
        {
            throw new InvalidOperationException("ABN format or checksum validation failed.");
        }

        var settings = options.Value;
        if (!settings.UseJsonLookup)
        {
            return normalized;
        }

        try
        {
            var client = httpClientFactory.CreateClient(AbrHttpClientName);
            var url = $"json/AbnDetails.aspx?abn={Uri.EscapeDataString(normalized)}";
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ABR lookup failed with status {Status} for ABN {Abn}", response.StatusCode, normalized);
                throw new InvalidOperationException("ABN Lookup API did not return success.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, default, cancellationToken);
            var root = doc.RootElement;
            if (!root.TryGetProperty("Abn", out var abnEl) ||
                !string.Equals(abnEl.GetString(), normalized, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("ABN Lookup API response did not match requested ABN.");
            }

            var statusCode = "Unknown";
            if (root.TryGetProperty("EntityStatus", out var es) &&
                es.TryGetProperty("EntityStatusCode", out var codeEl))
            {
                statusCode = codeEl.GetString() ?? "Unknown";
            }

            if (!string.Equals(statusCode, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"ABN entity status from ABR is '{statusCode}', expected Active.");
            }

            return normalized;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ABN Lookup API call failed for {Abn}", normalized);
            throw new InvalidOperationException("ABN Lookup API could not be reached or returned invalid data.");
        }
    }
}
