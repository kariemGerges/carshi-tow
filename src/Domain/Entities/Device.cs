using CarshiTow.Domain.ValueObjects;

namespace CarshiTow.Domain.Entities;

public sealed class Device
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DeviceFingerprint Fingerprint { get; set; } = new(string.Empty);
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = default!;
}
