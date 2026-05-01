using System.Security.Claims;
using CarshiTow.Api.Authorization;
using CarshiTow.Api.Middleware;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.PhotoPacks;
using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

[ApiController]
[Route("api/v1/packs")]
[Authorize(Policy = CarshiTowAuthorizationPolicies.MandatoryMfaEnrollment)]
[EnableRateLimiting(RateLimitingPolicies.DefaultPolicy)]
public sealed class PhotoPacksController(
    IPhotoPackService packs,
    IValidator<PhotoPackListQuery> listQueryValidator) : ControllerBase
{
    private Guid RequireUserId()
    {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PacksCreate)]
    [ProducesResponseType(typeof(PhotoPackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackDto>> Create([FromBody] CreatePhotoPackDraftRequest body, CancellationToken ct)
    {
        var dto = await packs.CreateDraftAsync(RequireUserId(), body, ct);
        return Ok(dto);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [ProducesResponseType(typeof(PhotoPackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackDto>> Update(
        Guid id,
        [FromBody] UpdatePhotoPackDraftRequest body,
        CancellationToken ct)
    {
        var dto = await packs.UpdateDraftAsync(RequireUserId(), id, body, ct);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await packs.DeleteDraftAsync(RequireUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [ProducesResponseType(typeof(PhotoPackPublishedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackPublishedResponse>> Publish(Guid id, CancellationToken ct)
    {
        var dto = await packs.PublishAsync(RequireUserId(), id, ct);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/regenerate-link")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [ProducesResponseType(typeof(PhotoPackPublishedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackPublishedResponse>> RegenerateLink(Guid id, CancellationToken ct)
    {
        var dto = await packs.RegenerateLinkAsync(RequireUserId(), id, ct);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = CarshiTowAuthorizationPolicies.TowYardPacksRead)]
    [ProducesResponseType(typeof(PhotoPackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await packs.GetByIdAsync(RequireUserId(), id, ct);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/stats")]
    [Authorize(Policy = CarshiTowAuthorizationPolicies.TowYardPacksRead)]
    [ProducesResponseType(typeof(PhotoPackStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackStatsDto>> Stats(Guid id, CancellationToken ct)
    {
        var dto = await packs.GetStatsAsync(RequireUserId(), id, ct);
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Policy = CarshiTowAuthorizationPolicies.TowYardPacksRead)]
    [ProducesResponseType(typeof(PhotoPackListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoPackListResponse>> List(
        [FromQuery] PhotoPackStatus? status,
        [FromQuery(Name = "createdFrom")] DateTime? createdFromUtc,
        [FromQuery(Name = "createdTo")] DateTime? createdToUtc,
        [FromQuery] int? minTowYardPriceCents,
        [FromQuery] int? maxTowYardPriceCents,
        [FromQuery] int pageSize = PhotoPackRules.DefaultListPageSize,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var query = new PhotoPackListQuery(
            status,
            createdFromUtc,
            createdToUtc,
            minTowYardPriceCents,
            maxTowYardPriceCents,
            pageSize,
            cursor);

        await listQueryValidator.ValidateAndThrowAsync(query, ct);

        var (items, next) = await packs.ListAsync(RequireUserId(), query, ct);
        return Ok(new PhotoPackListResponse(items, NextCursor: next));
    }
}

public sealed record PhotoPackListResponse(IReadOnlyList<PhotoPackDto> Items, string? NextCursor);
