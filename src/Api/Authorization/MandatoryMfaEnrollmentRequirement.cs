using Microsoft.AspNetCore.Authorization;

namespace CarshiTow.Api.Authorization;

/// <summary>
/// Requires <see cref="CarshiTow.Domain.Entities.User.IsMfaEnabled"/> for callers whose role expects MFA
/// (tow yard &amp; Crashify), so platform admin actions cannot bypass Enterprise 2.1 MFA expectations when MFA was disabled on the account.
/// </summary>
public sealed class MandatoryMfaEnrollmentRequirement : IAuthorizationRequirement
{
}