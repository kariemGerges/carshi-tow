using System.Security.Claims;
using CarshiTow.Api.Authorization;
using CarshiTow.Api.Middleware;
using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarshiTow.Api.Controllers;

[ApiController]
[Route("api/v1/packs/{packId:guid}/photos")]
[Authorize(Policy = CarshiTowAuthorizationPolicies.MandatoryMfaEnrollment)]
public sealed class PackPhotosController(IPackPhotoManagementService photos) : ControllerBase
{
    private Guid RequireUserId()
    {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpPost("upload-url")]
    [Authorize(Policy = Permissions.UploadsCreate)]
    [EnableRateLimiting(RateLimitingPolicies.PhotoUploadPolicy)]
    [ProducesResponseType(typeof(RequestPhotoUploadSlotResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestPhotoUploadSlotResponse>> UploadUrl(
        Guid packId,
        [FromBody] RequestPhotoUploadSlotRequest body,
        CancellationToken ct)
    {
        var r = await photos.RequestUploadSlotAsync(RequireUserId(), packId, body, ct);
        return Ok(r);
    }

    [HttpPost("confirm")]
    [Authorize(Policy = Permissions.UploadsCreate)]
    [EnableRateLimiting(RateLimitingPolicies.PhotoUploadPolicy)]
    [ProducesResponseType(typeof(PhotoInPackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoInPackDto>> ConfirmUpload(
        Guid packId,
        [FromBody] ConfirmPhotoUploadRequest body,
        CancellationToken ct)
    {
        var r = await photos.ConfirmUploadAsync(RequireUserId(), packId, body, ct);
        return Ok(r);
    }

    [HttpGet]
    [Authorize(Policy = CarshiTowAuthorizationPolicies.TowYardPacksRead)]
    [EnableRateLimiting(RateLimitingPolicies.DefaultPolicy)]
    [ProducesResponseType(typeof(IReadOnlyList<PhotoInPackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PhotoInPackDto>>> List(Guid packId, CancellationToken ct)
    {
        var r = await photos.ListPhotosAsync(RequireUserId(), packId, ct);
        return Ok(r);
    }

    [HttpPatch("{photoId:guid}")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [EnableRateLimiting(RateLimitingPolicies.DefaultPolicy)]
    [ProducesResponseType(typeof(PhotoInPackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoInPackDto>> Patch(
        Guid packId,
        Guid photoId,
        [FromBody] UpdatePhotoInPackRequest body,
        CancellationToken ct)
    {
        var r = await photos.PatchPhotoAsync(RequireUserId(), packId, photoId, body, ct);
        return Ok(r);
    }

    [HttpDelete("{photoId:guid}")]
    [Authorize(Policy = Permissions.PacksCreate)]
    [EnableRateLimiting(RateLimitingPolicies.DefaultPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid packId, Guid photoId, CancellationToken ct)
    {
        await photos.DeletePhotoAsync(RequireUserId(), packId, photoId, ct);
        return NoContent();
    }
}
