using CarshiTow.Api.Middleware;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

/// <summary>Anonymous insurer-facing link resolution (SRS §7.5 GET /links/:token).</summary>
[ApiController]
[Route("api/v1/links")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingPolicies.DefaultPolicy)]
public sealed class LinksController(IPublicPackLinkService links) : ControllerBase
{
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(PackLinkPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string token, CancellationToken cancellationToken)
    {
        var dto = await links.GetPreviewAsync(token, incrementViewCount: true, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}
