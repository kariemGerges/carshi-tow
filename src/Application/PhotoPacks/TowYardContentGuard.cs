using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.PhotoPacks;

/// <summary>SRS §TY-005 — no pack or photo mutations until tow yard is <see cref="TowYardStatus.Active"/> (admin approved).</summary>
public static class TowYardContentGuard
{
    public static void EnsureYardActiveForContentWrites(TowYardPartyContext ctx)
    {
        if (ctx.YardStatus != TowYardStatus.Active)
        {
            throw new InvalidOperationException(
                "Tow yard must be verified and active before creating packs, uploading photos, or editing draft content.");
        }
    }
}
