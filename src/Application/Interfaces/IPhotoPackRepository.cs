using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public sealed record PhotoPackListCriteria(
    PhotoPackStatus? Status,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    int? MinTowYardPriceCents,
    int? MaxTowYardPriceCents,
    Guid? CreatedByUserId);

public interface IPhotoPackRepository
{
    Task AddAsync(PhotoPack pack, CancellationToken cancellationToken);

    Task<PhotoPack?> GetTrackedPackAsync(Guid packId, CancellationToken cancellationToken);

    Task<PhotoPack?> GetTrackedPackWithPhotosAsync(Guid packId, CancellationToken cancellationToken);

    Task<int> MarkExpiredUnpaidPacksForYardAsync(Guid towYardId, DateTime utcNow, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PhotoPack> Items, string? NextCursor)> ListAsync(
        Guid towYardId,
        PhotoPackListCriteria criteria,
        int take,
        string? cursor,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<PhotoPack?> GetTrackedByLinkHashAsync(string linkTokenHash, CancellationToken cancellationToken);

    Task<Photo?> GetTrackedPhotoInPackAsync(Guid packId, Guid photoId, CancellationToken cancellationToken);

    Task AddPhotoAsync(Photo photo, CancellationToken cancellationToken);

    Task<int> CountActivePhotosAsync(Guid packId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Photo>> ListActivePhotosUntrackedAsync(Guid packId, CancellationToken cancellationToken);
}
