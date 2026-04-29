using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Security;

/// <summary>Determines when SMS OTP is required during sign-in (Enterprise 2.1 MFA expectations).</summary>
public static class MfaLoginRules
{
    /// <summary>
    /// Challenge with SMS OTP when the device is not yet trusted. If the device is trusted, challenge only when
    /// multi-factor enrollment is switched on so returning users skip OTP only when MFA is off (e.g. optional insurer flows).
    /// </summary>
    public static bool ShouldChallengeSmsOtp(User user, bool trustedDevice)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!trustedDevice)
        {
            return true;
        }

        return user.IsMfaEnabled;
    }
}
