using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.DTOs;

public sealed record PackPreviewPhotoDto(
    Guid Id,
    string PreviewUrl,
    short SortOrder);

public sealed record PackLinkPreviewDto(
    Guid PackId,
    string VehicleRego,
    AustralianState VehicleRegoState,
    string VehicleMake,
    string VehicleModel,
    short VehicleYear,
    DamageSeverity? DamageSeverity,
    short PhotoCount,
    int TotalPriceCents,
    int TowYardPriceCents,
    int PlatformFeeCents,
    PhotoPackStatus Status,
    bool LinkExpired,
    bool IsPaid,
    DateTime? LinkExpiresAtUtc,
    IReadOnlyList<PackPreviewPhotoDto> PreviewPhotos);
