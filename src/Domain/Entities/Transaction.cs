using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; set; }
    public Guid PackId { get; set; }
    public Guid TowYardId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string? StripeChargeId { get; set; }
    public int TotalAmountCents { get; set; }
    public int PlatformFeeCents { get; set; }
    public int TowYardAmountCents { get; set; }
    public int StripeFeeCents { get; set; }
    public int NetToTowYardCents { get; set; }
    public string InsurerEmail { get; set; } = string.Empty;
    public string? InsurerOrgName { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public int? RefundAmountCents { get; set; }
    public string? RefundReason { get; set; }
    public Guid? RefundedByUserId { get; set; }
    public string? InvoiceS3Key { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public PhotoPack Pack { get; set; } = default!;
    public TowYard TowYard { get; set; } = default!;
    public User? RefundedByUser { get; set; }
}
