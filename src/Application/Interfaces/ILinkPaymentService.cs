using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

/// <summary>SRS §7.5 — anonymous insurer payment + unlock + downloads.</summary>
public interface ILinkPaymentService
{
    Task<CreateLinkPaymentIntentResponse?> CreatePaymentIntentAsync(string rawLinkToken, CancellationToken cancellationToken);

    Task<ConfirmLinkPaymentResponse?> ConfirmPaymentAsync(
        string rawLinkToken,
        ConfirmLinkPaymentRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PackDownloadPhotoDto>?> GetPaidDownloadsAsync(
        string rawLinkToken,
        string paymentIntentId,
        CancellationToken cancellationToken);

    Task<LinkInvoicePdfResponse?> GetInvoicePdfAsync(string rawLinkToken, string paymentIntentId, CancellationToken cancellationToken);
}
