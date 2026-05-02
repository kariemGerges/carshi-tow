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
public sealed class LinksController(IPublicPackLinkService links, ILinkPaymentService payments) : ControllerBase
{
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(PackLinkPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string token, CancellationToken cancellationToken)
    {
        var dto = await links.GetPreviewAsync(token, incrementViewCount: true, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>SRS §7.5 — create Stripe PaymentIntent for anonymous insurer checkout.</summary>
    [HttpPost("{token}/payment-intent")]
    [ProducesResponseType(typeof(CreateLinkPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntent(string token, CancellationToken cancellationToken)
    {
        var dto = await payments.CreatePaymentIntentAsync(token, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>SRS §7.5 — verify payment server-side and unlock downloads.</summary>
    [HttpPost("{token}/payment-confirm")]
    [ProducesResponseType(typeof(ConfirmLinkPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment(
        string token,
        [FromBody] ConfirmLinkPaymentRequest body,
        CancellationToken cancellationToken)
    {
        var dto = await payments.ConfirmPaymentAsync(token, body, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>SRS §7.5 — signed GET URLs for originals (7-day access window after payment).</summary>
    [HttpGet("{token}/downloads")]
    [ProducesResponseType(typeof(IReadOnlyList<PackDownloadPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Downloads(string token, [FromQuery] string paymentIntentId, CancellationToken cancellationToken)
    {
        var dto = await payments.GetPaidDownloadsAsync(token, paymentIntentId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>SRS §IN-010 — tax invoice PDF after payment.</summary>
    [HttpGet("{token}/invoice")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Invoice(string token, [FromQuery] string paymentIntentId, CancellationToken cancellationToken)
    {
        var dto = await payments.GetInvoicePdfAsync(token, paymentIntentId, cancellationToken);
        return dto is null ? NotFound() : File(dto.PdfBytes, "application/pdf", dto.FileName);
    }
}
