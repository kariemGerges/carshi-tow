using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

public interface IPackPhotoManagementService
{
    Task<RequestPhotoUploadSlotResponse> RequestUploadSlotAsync(
        Guid actorUserId,
        Guid packId,
        RequestPhotoUploadSlotRequest request,
        CancellationToken cancellationToken);

    Task<PhotoInPackDto> ConfirmUploadAsync(
        Guid actorUserId,
        Guid packId,
        ConfirmPhotoUploadRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PhotoInPackDto>> ListPhotosAsync(
        Guid actorUserId,
        Guid packId,
        CancellationToken cancellationToken);

    Task<PhotoInPackDto> PatchPhotoAsync(
        Guid actorUserId,
        Guid packId,
        Guid photoId,
        UpdatePhotoInPackRequest request,
        CancellationToken cancellationToken);

    Task DeletePhotoAsync(
        Guid actorUserId,
        Guid packId,
        Guid photoId,
        CancellationToken cancellationToken);
}
