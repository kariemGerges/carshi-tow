using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class PhotoPackRepository(AppDbContext db) : IPhotoPackRepository
{
    public Task AddAsync(PhotoPack pack, CancellationToken cancellationToken) =>
        db.PhotoPacks.AddAsync(pack, cancellationToken).AsTask();

    public async Task<PhotoPack?> GetTrackedPackAsync(Guid packId, CancellationToken cancellationToken) =>
        await db.PhotoPacks
            .FirstOrDefaultAsync(p => p.Id == packId && p.DeletedAtUtc == null, cancellationToken);

    public async Task<PhotoPack?> GetTrackedPackWithPhotosAsync(Guid packId, CancellationToken cancellationToken) =>
        await db.PhotoPacks
            .Include(x => x.Photos
                .Where(ph => ph.DeletedAtUtc == null)
                .OrderBy(ph => ph.SortOrder)
                .ThenBy(ph => ph.Id))
            .FirstOrDefaultAsync(p => p.Id == packId && p.DeletedAtUtc == null, cancellationToken);

    public Task<PhotoPack?> GetTrackedByLinkHashAsync(string linkTokenHash, CancellationToken cancellationToken) =>
        db.PhotoPacks
            .Include(x => x.Photos
                .Where(ph => ph.DeletedAtUtc == null)
                .OrderBy(ph => ph.SortOrder)
                .ThenBy(ph => ph.Id))
            .FirstOrDefaultAsync(p => p.LinkToken == linkTokenHash && p.DeletedAtUtc == null, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<Photo?> GetTrackedPhotoInPackAsync(Guid packId, Guid photoId, CancellationToken cancellationToken) =>
        await db.Photos
            .FirstOrDefaultAsync(
                p => p.PackId == packId && p.Id == photoId && p.DeletedAtUtc == null,
                cancellationToken);

    public Task AddPhotoAsync(Photo photo, CancellationToken cancellationToken) =>
        db.Photos.AddAsync(photo, cancellationToken).AsTask();

    public Task<int> CountActivePhotosAsync(Guid packId, CancellationToken cancellationToken) =>
        db.Photos.CountAsync(p => p.PackId == packId && p.DeletedAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<Photo>> ListActivePhotosUntrackedAsync(
        Guid packId,
        CancellationToken cancellationToken) =>
        await db.Photos.AsNoTracking()
            .Where(p => p.PackId == packId && p.DeletedAtUtc == null)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<int> MarkExpiredUnpaidPacksForYardAsync(
        Guid towYardId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await db.PhotoPacks
            .Where(p =>
                p.TowYardId == towYardId
                && p.DeletedAtUtc == null
                && p.Status == PhotoPackStatus.Active
                && p.PaidAtUtc == null
                && p.LinkExpiresAtUtc != null
                && p.LinkExpiresAtUtc < utcNow)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, PhotoPackStatus.Expired)
                    .SetProperty(p => p.UpdatedAtUtc, utcNow),
                cancellationToken);

    public async Task<(IReadOnlyList<PhotoPack> Items, string? NextCursor)> ListAsync(
        Guid towYardId,
        PhotoPackListCriteria criteria,
        int take,
        string? cursor,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        DateTimeOffset? cursorCreated = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(cursor) && CursorCodec.TryDecode(cursor, out var cAt, out var cGuid))
        {
            cursorCreated = new DateTimeOffset(DateTime.SpecifyKind(cAt, DateTimeKind.Utc));
            cursorId = cGuid;
        }

        var q = db.PhotoPacks.AsNoTracking().Where(p => p.TowYardId == towYardId && p.DeletedAtUtc == null);

        if (criteria.CreatedByUserId is Guid byUser)
        {
            q = q.Where(p => p.CreatedByUserId == byUser);
        }

        if (criteria.Status is PhotoPackStatus st)
        {
            q = q.Where(p => p.Status == st);
        }

        if (criteria.CreatedFromUtc is DateTime from)
        {
            q = q.Where(p => p.CreatedAtUtc >= from);
        }

        if (criteria.CreatedToUtc is DateTime to)
        {
            q = q.Where(p => p.CreatedAtUtc <= to);
        }

        if (criteria.MinTowYardPriceCents is int minP)
        {
            q = q.Where(p => p.TowYardPriceCents >= minP);
        }

        if (criteria.MaxTowYardPriceCents is int maxP)
        {
            q = q.Where(p => p.TowYardPriceCents <= maxP);
        }

        if (cursorCreated.HasValue && cursorId.HasValue)
        {
            var cUtc = cursorCreated.Value.UtcDateTime;
            var cG = cursorId.Value;
            q = q.Where(p =>
                p.CreatedAtUtc < cUtc
                || (p.CreatedAtUtc == cUtc && p.Id.CompareTo(cG) < 0));
        }

        q = q.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id);

        var items = await q.Take(take + 1).ToListAsync(cancellationToken);

        string? next = null;
        if (items.Count > take)
        {
            items.RemoveAt(take);
            var last = items[^1];
            next = CursorCodec.Encode(last.CreatedAtUtc, last.Id);
        }

        return (items, next);
    }

    private static class CursorCodec
    {
        public static string Encode(DateTime createdAtUtc, Guid id)
        {
            var iso = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            var raw = $"{iso}|{id:D}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        }

        public static bool TryDecode(string cursor, out DateTime createdAtUtc, out Guid id)
        {
            createdAtUtc = default;
            id = default;
            byte[] buf;
            try
            {
                buf = Convert.FromBase64String(cursor);
            }
            catch
            {
                return false;
            }

            var s = System.Text.Encoding.UTF8.GetString(buf);
            var pipe = s.IndexOf('|', StringComparison.Ordinal);
            if (pipe <= 0)
            {
                return false;
            }

            var iso = s[..pipe];
            var idPart = s[(pipe + 1)..];
            if (!DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out createdAtUtc))
            {
                return false;
            }

            createdAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
            return Guid.TryParse(idPart, out id);
        }
    }
}
