using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.PhotoPacks;

public static class PhotoPackAccess
{
    public static void EnsurePackForActor(TowYardPartyContext ctx, PhotoPack pack, Guid actorUserId)
    {
        if (pack.TowYardId != ctx.TowYardId)
        {
            throw new UnauthorizedAccessException();
        }

        if (ctx.Visibility == TowYardVisibilityScope.AssignedPacksOnly && pack.CreatedByUserId != actorUserId)
        {
            throw new UnauthorizedAccessException();
        }
    }
}
