using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class PhotoPack
{
    public Guid Id { get; set; }
    public Guid TowYardId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string VehicleRego { get; set; } = string.Empty;
    public AustralianState VehicleRegoState { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public short VehicleYear { get; set; }
    public string? VehicleVin { get; set; }
    public string? ClaimReference { get; set; }
    public string? TowYardReference { get; set; }
    public PhotoPackStatus Status { get; set; } = PhotoPackStatus.Draft;
    public short PhotoCount { get; set; }
    public short? QualityScore { get; set; }
    public DamageSeverity? DamageSeverity { get; set; }
    public decimal? TotalLossProbability { get; set; }
    public string? AiDamageDescription { get; set; }
    public int TowYardPriceCents { get; set; }
    public int PlatformFeeCents { get; set; } = 5500;
    public int TotalPriceCents { get; set; }
    public string? LinkToken { get; set; }
    public DateTime? LinkExpiresAtUtc { get; set; }
    public int LinkViewCount { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PaidByEmail { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public bool FraudFlagged { get; set; }
    public string? FraudFlaggedReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public TowYard TowYard { get; set; } = default!;
    public User CreatedByUser { get; set; } = default!;
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
