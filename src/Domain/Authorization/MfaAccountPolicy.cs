using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Authorization;

/// <summary>Which roles must keep MFA enabled on the account to use privileged platform APIs.</summary>
public static class MfaAccountPolicy
{
    /// <summary>Insurer accounts may operate with MFA off (link-first / optional account). All other roles require enrollment for platform operations that use this policy.</summary>
    public static bool RoleRequiresMfaEnabledForPrivilegedApis(UserRole role) => role != UserRole.Insurer;
}
