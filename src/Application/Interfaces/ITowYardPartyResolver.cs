using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public enum TowYardVisibilityScope
{
    YardWide,
    AssignedPacksOnly
}

/// <summary>Resolved tow-yard context for the authenticated employee (SRS §2.2).</summary>
public sealed record TowYardPartyContext(
    Guid TowYardId,
    TowYardVisibilityScope Visibility,
    TowYardStatus YardStatus,
    int PlatformFeeCents);

public interface ITowYardPartyResolver
{
    Task<TowYardPartyContext> ResolveAsync(Guid userId, CancellationToken cancellationToken);
}
