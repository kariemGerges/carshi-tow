using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class Payout
{
    public Guid Id { get; set; }
    public Guid TowYardId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public short TransactionCount { get; set; }
    public int GrossAmountCents { get; set; }
    public int ProcessingFeeCents { get; set; }
    public int NetAmountCents { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? BankReference { get; set; }
    public DateTime? InitiatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public TowYard TowYard { get; set; } = default!;
}
