using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class TowYardRepository(AppDbContext db) : ITowYardRepository
{
    public Task<bool> ExistsActiveAbnAsync(string canonicalAbnDigits, CancellationToken cancellationToken) =>
        db.TowYards.AnyAsync(
            t => t.Abn == canonicalAbnDigits && t.DeletedAtUtc == null,
            cancellationToken);

    public Task AddAsync(TowYard towYard, CancellationToken cancellationToken) =>
        db.TowYards.AddAsync(towYard, cancellationToken).AsTask();
}
