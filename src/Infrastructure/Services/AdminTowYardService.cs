using CarshiTow.Application.DTOs;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Services;

public sealed class AdminTowYardService(AppDbContext db) : IAdminTowYardService
{
    public async Task<AdminTowYardListResponseDto> ListAsync(
        TowYardStatus? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.TowYards.AsNoTracking()
            .Where(y => y.DeletedAtUtc == null)
            .Join(db.Users.AsNoTracking(), y => y.OwnerUserId, u => u.Id, (y, u) => new { Yard = y, Owner = u })
            .Where(x => x.Owner.DeletedAtUtc == null);

        if (statusFilter is not null)
        {
            q = q.Where(x => x.Yard.Status == statusFilter);
        }

        var total = await q.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;
        var rows = await q
            .OrderByDescending(x => x.Yard.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new TowYardAdminListItemDto(
                x.Yard.Id,
                x.Yard.BusinessName,
                x.Yard.Abn,
                x.Yard.Status,
                x.Yard.CreatedAtUtc,
                x.Owner.Email,
                x.Owner.Id))
            .ToListAsync(cancellationToken);

        return new AdminTowYardListResponseDto(rows, total, page, pageSize);
    }

    public async Task UpdateStatusAsync(
        Guid adminUserId,
        Guid towYardId,
        AdminUpdateTowYardStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var yard = await db.TowYards
            .Include(y => y.OwnerUser)
            .FirstOrDefaultAsync(y => y.Id == towYardId && y.DeletedAtUtc == null, cancellationToken)
            ?? throw new KeyNotFoundException("Tow yard not found.");

        var owner = yard.OwnerUser;
        if (owner.DeletedAtUtc is not null)
        {
            throw new InvalidOperationException("Owner user is deleted.");
        }

        var utc = DateTime.UtcNow;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        switch (request.Status)
        {
            case TowYardStatus.Active:
            {
                var previous = yard.Status;
                if (previous is not (TowYardStatus.Pending or TowYardStatus.Rejected or TowYardStatus.Suspended))
                {
                    throw new InvalidOperationException("Can only activate a pending, rejected, or suspended tow yard.");
                }

                yard.Status = TowYardStatus.Active;
                yard.LastStatusChangeReason = null;
                if (previous is TowYardStatus.Pending or TowYardStatus.Rejected)
                {
                    yard.VerifiedAtUtc = utc;
                    yard.VerifiedByUserId = adminUserId;
                }

                owner.Status = UserStatus.Active;
                owner.UpdatedAtUtc = utc;
                break;
            }

            case TowYardStatus.Rejected:
                if (yard.Status != TowYardStatus.Pending)
                {
                    throw new InvalidOperationException("Can only reject a pending registration.");
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new InvalidOperationException("Reason is required to reject a tow yard.");
                }

                yard.Status = TowYardStatus.Rejected;
                yard.LastStatusChangeReason = reason;
                yard.VerifiedAtUtc = null;
                yard.VerifiedByUserId = null;
                owner.UpdatedAtUtc = utc;
                break;

            case TowYardStatus.Suspended:
                if (yard.Status != TowYardStatus.Active)
                {
                    throw new InvalidOperationException("Can only suspend an active tow yard.");
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new InvalidOperationException("Reason is required to suspend a tow yard.");
                }

                yard.Status = TowYardStatus.Suspended;
                yard.LastStatusChangeReason = reason;
                yard.UpdatedAtUtc = utc;
                break;

            default:
                throw new InvalidOperationException("Unsupported target status.");
        }

        yard.UpdatedAtUtc = utc;
        await db.SaveChangesAsync(cancellationToken);
    }
}
