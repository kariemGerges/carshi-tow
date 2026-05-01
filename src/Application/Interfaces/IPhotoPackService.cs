using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

public interface IPhotoPackService
{
    Task<PhotoPackDto> CreateDraftAsync(Guid actorUserId, CreatePhotoPackDraftRequest request, CancellationToken cancellationToken);

    Task<PhotoPackDto> UpdateDraftAsync(Guid actorUserId, Guid packId, UpdatePhotoPackDraftRequest request, CancellationToken cancellationToken);

    Task DeleteDraftAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken);

    Task<PhotoPackPublishedResponse> PublishAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken);

    Task<PhotoPackPublishedResponse> RegenerateLinkAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken);

    Task<PhotoPackDto> GetByIdAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PhotoPackDto> Items, string? NextCursor)> ListAsync(
        Guid actorUserId,
        PhotoPackListQuery query,
        CancellationToken cancellationToken);

    Task<PhotoPackStatsDto> GetStatsAsync(Guid actorUserId, Guid packId, CancellationToken cancellationToken);
}
