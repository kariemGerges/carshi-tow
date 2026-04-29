using CarshiTow.Domain.ValueObjects;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public HashedPassword Password { get; set; } = new(string.Empty);
    public bool IsMfaEnabled { get; set; } = true;
    public string? MfaSecret { get; set; }
    public UserRole Role { get; set; } = UserRole.TowYardStaff;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<TowYard> OwnedTowYards { get; set; } = new List<TowYard>();
    public ICollection<TowYard> VerifiedTowYards { get; set; } = new List<TowYard>();
    public ICollection<PhotoPack> CreatedPhotoPacks { get; set; } = new List<PhotoPack>();
    public ICollection<Transaction> RefundedTransactions { get; set; } = new List<Transaction>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
