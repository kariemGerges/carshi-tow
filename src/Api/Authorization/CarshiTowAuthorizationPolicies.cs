using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CarshiTow.Api.Authorization;

/// <summary>Registers permission-based policies (policy name equals permission string) and Crashify-only role policies.</summary>
public static class CarshiTowAuthorizationPolicies
{
    public static readonly string CrashifyAdminOnly = nameof(CrashifyAdminOnly);
    public static readonly string CrashifyPlatform = nameof(CrashifyPlatform);

    /// <summary>Requires MFA enabled for tow-yard and Crashify roles (insurer exempt).</summary>
    public const string MandatoryMfaEnrollment = "MandatoryMfaEnrollment";

    public static void AddPolicies(AuthorizationOptions options)
    {
        foreach (var perm in Permissions.All)
        {
            options.AddPolicy(perm,
                policy => policy.RequireClaim(PermissionClaimTypes.Permission, perm));
        }

        options.AddPolicy(CrashifyAdminOnly, policy =>
        {
            policy.RequireRole(nameof(UserRole.CrashifyAdmin));
        });

        options.AddPolicy(CrashifyPlatform, policy =>
        {
            policy.RequireRole(nameof(UserRole.CrashifyAdmin), nameof(UserRole.CrashifySupport));
        });

        options.AddPolicy(MandatoryMfaEnrollment, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MandatoryMfaEnrollmentRequirement());
        });
    }
}
