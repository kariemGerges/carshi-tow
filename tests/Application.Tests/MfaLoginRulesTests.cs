using CarshiTow.Application.Security;
using CarshiTow.Domain.Entities;

namespace Application.Tests;

public sealed class MfaLoginRulesTests
{
    [Fact]
    public void Unknown_device_always_challenges()
    {
        var u = new User { IsMfaEnabled = false };
        Assert.True(MfaLoginRules.ShouldChallengeSmsOtp(u, trustedDevice: false));
    }

    [Fact]
    public void Trusted_device_challenges_only_when_mfa_enabled()
    {
        var off = new User { IsMfaEnabled = false };
        var on = new User { IsMfaEnabled = true };
        Assert.False(MfaLoginRules.ShouldChallengeSmsOtp(off, trustedDevice: true));
        Assert.True(MfaLoginRules.ShouldChallengeSmsOtp(on, trustedDevice: true));
    }
}
