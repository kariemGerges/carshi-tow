namespace CarshiTow.Application.Configuration;

/// <summary>Optional live ABN Lookup (ABR JSON API). Checksum always runs locally first.</summary>
public sealed class AbnLookupSettings
{
    public const string SectionName = "AbnLookup";

    /// <summary>If true, validates format + checksum then calls <see cref="JsonApiGuid"/> REST endpoint pattern.</summary>
    public bool UseJsonLookup { get; set; }

    /// <summary>
    /// ABR JSON GUID query template. Example: append <c>{guid}/abn/details</c>?abn=<c>{{abn}}</c> per documentation.
    /// Leave empty when <see cref="UseJsonLookup"/> is false (default for dev).
    /// </summary>
    public Uri? JsonLookupBaseUri { get; set; }

    /// <summary>Overall timeout when calling JSON lookup.</summary>
    public int TimeoutSeconds { get; set; } = 20;
}
