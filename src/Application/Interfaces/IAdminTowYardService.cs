using CarshiTow.Application.DTOs;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public interface IAdminTowYardService
{
    Task<AdminTowYardListResponseDto> ListAsync(
        TowYardStatus? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        Guid adminUserId,
        Guid towYardId,
        AdminUpdateTowYardStatusRequestDto request,
        CancellationToken cancellationToken);
}
