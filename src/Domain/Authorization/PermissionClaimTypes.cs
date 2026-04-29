namespace CarshiTow.Domain.Authorization;

/// <summary>Claim types used on JWT access tokens and <see cref="System.Security.Claims.ClaimsPrincipal"/>.</summary>
public static class PermissionClaimTypes
{
    /// <summary>Granular RBAC permission; may appear multiple times on one token.</summary>
    public const string Permission = "permission";
}
