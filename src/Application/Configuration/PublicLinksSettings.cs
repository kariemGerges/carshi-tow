namespace CarshiTow.Application.Configuration;

/// <summary>Insurer-facing share URLs (SRS §7.5 public link flow).</summary>
public sealed class PublicLinksSettings
{
    public const string SectionName = "PublicLinks";

    /// <summary>Base URL for the insurer web app path that hosts the token (no trailing slash required).</summary>
    public string InsurerAccessBaseUrl { get; set; } = "https://localhost:5173/access";
}
