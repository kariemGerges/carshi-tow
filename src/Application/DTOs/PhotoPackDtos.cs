using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.DTOs;

public sealed record CreatePhotoPackDraftRequest(
    string VehicleRego,
    AustralianState VehicleRegoState,
    string VehicleMake,
    string VehicleModel,
    short VehicleYear,
    string? VehicleVin,
    string? ClaimReference,
    string? TowYardReference,
    int TowYardPriceCents);

public sealed record UpdatePhotoPackDraftRequest(
    string? VehicleRego,
    AustralianState? VehicleRegoState,
    string? VehicleMake,
    string? VehicleModel,
    short? VehicleYear,
    string? VehicleVin,
    string? ClaimReference,
    string? TowYardReference,
    int? TowYardPriceCents);

public sealed record PhotoPackListQuery(
    PhotoPackStatus? Status,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    int? MinTowYardPriceCents,
    int? MaxTowYardPriceCents,
    int PageSize,
    string? Cursor);

public sealed record PhotoPackDto(
    Guid Id,
    Guid TowYardId,
    Guid CreatedByUserId,
    string VehicleRego,
    AustralianState VehicleRegoState,
    string VehicleMake,
    string VehicleModel,
    short VehicleYear,
    string? VehicleVin,
    string? ClaimReference,
    string? TowYardReference,
    PhotoPackStatus Status,
    short PhotoCount,
    short? QualityScore,
    DamageSeverity? DamageSeverity,
    int TowYardPriceCents,
    int PlatformFeeCents,
    int TotalPriceCents,
    DateTime? LinkExpiresAtUtc,
    int LinkViewCount,
    DateTime? PaidAtUtc,
    bool FraudFlagged,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PhotoPackPublishedResponse(
    PhotoPackDto Pack,
    string ShareUrl,
    DateTime LinkExpiresAtUtc);

public sealed record PhotoPackStatsDto(
    Guid PackId,
    int LinkViewCount,
    bool IsPaid,
    DateTime? PaidAtUtc,
    PhotoPackStatus Status,
    DateTime? LinkExpiresAtUtc);
