using CarshiTow.Application.DTOs;

namespace CarshiTow.Application.Interfaces;

public interface ITowYardDashboardService
{
    Task<TowYardDashboardSummaryDto?> GetDashboardForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TowYardPayoutListItemDto>> ListPayoutsAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken);

    Task<TowYardPayoutBalanceDto?> GetPayoutBalanceAsync(Guid userId, CancellationToken cancellationToken);
}
