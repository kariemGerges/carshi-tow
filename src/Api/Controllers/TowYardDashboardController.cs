using System.Security.Claims;
using CarshiTow.Api.Authorization;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

/// <summary>Tow yard dashboard and payout read APIs (authenticated yard admin/staff).</summary>
[ApiController]
[Route("api/v1/me/tow-yard")]
[Authorize(Policy = Permissions.DashboardView)]
[Authorize(Policy = CarshiTowAuthorizationPolicies.MandatoryMfaEnrollment)]
public sealed class TowYardDashboardController(ITowYardDashboardService dashboard) : ControllerBase
{
    [HttpGet("dashboard")]
    [EnableRateLimiting(Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(typeof(TowYardDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var dto = await dashboard.GetDashboardForUserAsync(userId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("payouts")]
    [EnableRateLimiting(Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(typeof(IReadOnlyList<TowYardPayoutListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListPayouts([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (await dashboard.GetPayoutBalanceAsync(userId, cancellationToken) is null)
        {
            return NotFound();
        }

        var list = await dashboard.ListPayoutsAsync(userId, take, cancellationToken);
        return Ok(list);
    }

    [HttpGet("payouts/balance")]
    [EnableRateLimiting(Middleware.RateLimitingPolicies.AuthPolicy)]
    [ProducesResponseType(typeof(TowYardPayoutBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var dto = await dashboard.GetPayoutBalanceAsync(userId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }
}
