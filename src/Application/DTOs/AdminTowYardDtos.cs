using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.DTOs;

public sealed record TowYardAdminListItemDto(
    Guid Id,
    string BusinessName,
    string Abn,
    TowYardStatus Status,
    DateTime CreatedAtUtc,
    string OwnerEmail,
    Guid OwnerUserId);

public sealed record AdminTowYardListResponseDto(
    IReadOnlyList<TowYardAdminListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminUpdateTowYardStatusRequestDto(
    TowYardStatus Status,
    string? Reason);
