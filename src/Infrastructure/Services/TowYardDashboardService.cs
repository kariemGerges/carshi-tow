using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Services;

public sealed class TowYardDashboardService(AppDbContext db) : ITowYardDashboardService
{
    public async Task<TowYardDashboardSummaryDto?> GetDashboardForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var yardId = await ResolveTowYardIdAsync(userId, cancellationToken);
        if (yardId is null)
        {
            return null;
        }

        var yard = await db.TowYards.AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == yardId && y.DeletedAtUtc == null, cancellationToken);
        if (yard is null)
        {
            return null;
        }

        var since30 = DateTime.UtcNow.AddDays(-30);

        var packCounts = await db.PhotoPacks.AsNoTracking()
            .Where(p => p.TowYardId == yardId && p.DeletedAtUtc == null)
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);

        int C(PhotoPackStatus s) => packCounts.FirstOrDefault(x => x.Key == s)?.C ?? 0;

        var tx30 = await db.Transactions.AsNoTracking()
            .Where(t => t.TowYardId == yardId && t.DeletedAtUtc == null
                && t.Status == TransactionStatus.Succeeded
                && t.CreatedAtUtc >= since30)
            .ToListAsync(cancellationToken);

        var txAll = await db.Transactions.AsNoTracking()
            .Where(t => t.TowYardId == yardId && t.DeletedAtUtc == null && t.Status == TransactionStatus.Succeeded)
            .SumAsync(t => (int?)t.TotalAmountCents, cancellationToken) ?? 0;

        return new TowYardDashboardSummaryDto(
            yard.Id,
            yard.BusinessName,
            yard.Status,
            C(PhotoPackStatus.Draft),
            C(PhotoPackStatus.Active),
            C(PhotoPackStatus.Paid),
            C(PhotoPackStatus.Expired),
            C(PhotoPackStatus.Flagged),
            tx30.Count,
            tx30.Sum(t => t.TotalAmountCents),
            txAll);
    }

    public async Task<IReadOnlyList<TowYardPayoutListItemDto>> ListPayoutsAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken)
    {
        var yardId = await ResolveTowYardIdAsync(userId, cancellationToken);
        if (yardId is null)
        {
            return [];
        }

        take = Math.Clamp(take, 1, 100);
        return await db.Payouts.AsNoTracking()
            .Where(p => p.TowYardId == yardId && p.DeletedAtUtc == null)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(take)
            .Select(p => new TowYardPayoutListItemDto(
                p.Id,
                p.PeriodStart,
                p.PeriodEnd,
                p.TransactionCount,
                p.GrossAmountCents,
                p.NetAmountCents,
                p.Status,
                p.CompletedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<TowYardPayoutBalanceDto?> GetPayoutBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var yardId = await ResolveTowYardIdAsync(userId, cancellationToken);
        if (yardId is null)
        {
            return null;
        }

        var accrued = await db.Transactions.AsNoTracking()
            .Where(t => t.TowYardId == yardId && t.DeletedAtUtc == null && t.Status == TransactionStatus.Succeeded)
            .SumAsync(t => (int?)t.NetToTowYardCents, cancellationToken) ?? 0;

        var paidOut = await db.Payouts.AsNoTracking()
            .Where(p => p.TowYardId == yardId && p.DeletedAtUtc == null && p.Status == PayoutStatus.Completed)
            .SumAsync(p => (int?)p.NetAmountCents, cancellationToken) ?? 0;

        return new TowYardPayoutBalanceDto(accrued, paidOut, Math.Max(0, accrued - paidOut));
    }

    private async Task<Guid?> ResolveTowYardIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.OwnedTowYards)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAtUtc == null, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.Role == UserRole.TowYardAdmin)
        {
            return user.OwnedTowYards
                .Where(y => y.DeletedAtUtc == null)
                .OrderByDescending(y => y.CreatedAtUtc)
                .Select(y => y.Id)
                .FirstOrDefault();
        }

        if (user.Role == UserRole.TowYardStaff && user.TowYardId is Guid tid)
        {
            return tid;
        }

        return null;
    }
}
