using CarshiTow.Application.Interfaces;
using CarshiTow.Application.PhotoPacks;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Services;

public sealed class TowYardPartyResolver(AppDbContext db) : ITowYardPartyResolver
{
    public async Task<TowYardPartyContext> ResolveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ??
                   throw new UnauthorizedAccessException();

        switch (user.Role)
        {
            case UserRole.TowYardAdmin:
            {
                var yard = await db.TowYards.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.OwnerUserId == user.Id && t.DeletedAtUtc == null, cancellationToken);
                if (yard is null)
                {
                    throw new InvalidOperationException(
                        "No tow yard is linked to this account. Complete onboarding before managing photo packs.");
                }

                return new TowYardPartyContext(
                    yard.Id,
                    TowYardVisibilityScope.YardWide,
                    yard.Status,
                    yard.PlatformFeeOverride ?? PhotoPackRules.DefaultPlatformFeeCents);
            }
            case UserRole.TowYardStaff:
            {
                if (user.TowYardId is null)
                {
                    throw new InvalidOperationException(
                        "Staff account must be assigned to a tow yard before managing photo packs.");
                }

                var yard = await db.TowYards.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == user.TowYardId.Value && t.DeletedAtUtc == null, cancellationToken);

                return yard is null
                    ? throw new InvalidOperationException("Assigned tow yard could not be found.")
                    : new TowYardPartyContext(
                        yard.Id,
                        TowYardVisibilityScope.AssignedPacksOnly,
                        yard.Status,
                        yard.PlatformFeeOverride ?? PhotoPackRules.DefaultPlatformFeeCents);
            }
            default:
                throw new UnauthorizedAccessException();
        }
    }
}
