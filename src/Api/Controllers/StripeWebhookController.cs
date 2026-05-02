using CarshiTow.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace CarshiTow.Api.Controllers;

/// <summary>SRS §7.8 — Stripe webhook (belt-and-suspenders with §12.2).</summary>
[ApiController]
[Route("api/v1/webhooks/stripe")]
[AllowAnonymous]
public sealed class StripeWebhookController(IOptions<StripeSettings> stripeSettings, ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = stripeSettings.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("Stripe webhook received but WebhookSecret is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var sig))
        {
            return BadRequest("Missing Stripe-Signature header.");
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, sig, secret, throwOnApiVersionMismatch: false);
            logger.LogInformation("Stripe webhook {Type} id={Id}", stripeEvent.Type, stripeEvent.Id);
            // Idempotent side-effects (e.g. mark pack paid) are handled in ConfirmPayment; webhook is audit + future reconciliation.
            return Ok();
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature or payload.");
            return BadRequest();
        }
    }
}
