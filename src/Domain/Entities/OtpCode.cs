using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class OtpCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAtUtc { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc is not null;
}
