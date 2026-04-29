namespace CarshiTow.Application.Configuration;

/// <summary>Creates a single Crashify admin user when <see cref="Enabled"/> and credentials are set (use secrets in non-dev).</summary>
public sealed class CrashifySeedSettings
{
    public const string SectionName = "CrashifySeed";

    public bool Enabled { get; set; }

    /// <summary>Lowercased email for the seeded admin account.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Initial password; change after first login.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Must be unique in <c>users.phone</c>.</summary>
    public string PhoneNumber { get; set; } = "+61490000001";
}
