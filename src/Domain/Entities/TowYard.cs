using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class TowYard
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Abn { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public AustralianState State { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public TowYardStatus Status { get; set; } = TowYardStatus.Pending;
    public string[]? VerificationDocsUrl { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }

    /// <summary>Mandatory for reject/suspend admin actions (SRS AD-002 / AD-003).</summary>
    public string? LastStatusChangeReason { get; set; }
    public string? BankBsb { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? StripeConnectId { get; set; }
    public int? PlatformFeeOverride { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public User OwnerUser { get; set; } = default!;
    public User? VerifiedByUser { get; set; }
    public ICollection<PhotoPack> PhotoPacks { get; set; } = new List<PhotoPack>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();

    /// <summary>Staff users linked to this yard (SRS §2.2.1 TY-007).</summary>
    public ICollection<User> StaffUsers { get; set; } = new List<User>();
}
