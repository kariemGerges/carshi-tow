using CarshiTow.Api.Authorization;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

/// <summary>Platform user-management APIs (Crashify Admin only).</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = Permissions.PlatformUsersManage)]
[Authorize(Policy = CarshiTowAuthorizationPolicies.MandatoryMfaEnrollment)]
public sealed class AdminUsersController(IPlatformAdminService platformAdmin) : ControllerBase
{
    /// <summary>Assigns a new <see cref="UserRole"/> to the target user.</summary>
    [HttpPatch("{userId:guid}/role")]
    [EnableRateLimiting(CarshiTow.Api.Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        await platformAdmin.AssignUserRoleAsync(userId, role, cancellationToken);
        return NoContent();
    }
}
