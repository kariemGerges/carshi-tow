using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;

namespace Domain.Tests;

public sealed class MfaAccountPolicyTests
{
    [Fact]
    public void Insurer_privileged_policy_exempt_otherwise_required()
    {
        Assert.False(MfaAccountPolicy.RoleRequiresMfaEnabledForPrivilegedApis(UserRole.Insurer));
        Assert.True(MfaAccountPolicy.RoleRequiresMfaEnabledForPrivilegedApis(UserRole.TowYardStaff));
        Assert.True(MfaAccountPolicy.RoleRequiresMfaEnabledForPrivilegedApis(UserRole.CrashifyAdmin));
    }
}
