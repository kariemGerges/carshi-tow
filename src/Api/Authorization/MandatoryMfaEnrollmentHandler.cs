using System.Security.Claims;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CarshiTow.Api.Authorization;

public sealed class MandatoryMfaEnrollmentHandler(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<MandatoryMfaEnrollmentRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MandatoryMfaEnrollmentRequirement requirement)
    {
        _ = requirement;
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var sub = context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
        {
            return;
        }

        var ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return;
        }

        if (!MfaAccountPolicy.RoleRequiresMfaEnabledForPrivilegedApis(user.Role))
        {
            context.Succeed(requirement);
            return;
        }

        if (user.IsMfaEnabled)
        {
            context.Succeed(requirement);
        }
    }
}
