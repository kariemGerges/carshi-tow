using System.Security.Claims;
using CarshiTow.Api.Authorization;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

/// <summary>Crashify Admin — tow yard verification and status (SRS AD-001–003).</summary>
[ApiController]
[Route("api/v1/admin/tow-yards")]
[Authorize(Policy = Permissions.PlatformTowYardsManage)]
[Authorize(Policy = CarshiTowAuthorizationPolicies.MandatoryMfaEnrollment)]
public sealed class AdminTowYardsController(IAdminTowYardService adminTowYards) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(typeof(AdminTowYardListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] TowYardStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminTowYards.ListAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{towYardId:guid}/status")]
    [EnableRateLimiting(Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid towYardId,
        [FromBody] AdminUpdateTowYardStatusRequestDto body,
        CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var adminUserId))
        {
            return Unauthorized();
        }

        await adminTowYards.UpdateStatusAsync(adminUserId, towYardId, body, cancellationToken);
        return NoContent();
    }
}
