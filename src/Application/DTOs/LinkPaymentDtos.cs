namespace CarshiTow.Application.DTOs;

public sealed record CreateLinkPaymentIntentResponse(string ClientSecret, string PaymentIntentId, int AmountCents, string Currency);

public sealed record ConfirmLinkPaymentRequest(string PaymentIntentId, string? InsurerEmail);

public sealed record ConfirmLinkPaymentResponse(
    IReadOnlyList<PackDownloadPhotoDto> Downloads,
    DateTime DownloadAccessExpiresAtUtc);

public sealed record PackDownloadPhotoDto(Guid PhotoId, string FileName, string SignedGetUrl, DateTime UrlExpiresAtUtc);

public sealed record LinkInvoicePdfResponse(byte[] PdfBytes, string FileName);
