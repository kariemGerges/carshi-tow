using CarshiTow.Domain.Entities;

namespace CarshiTow.Application.Interfaces;

public interface ITowYardRepository
{
    Task<bool> ExistsActiveAbnAsync(string canonicalAbnDigits, CancellationToken cancellationToken);

    Task AddAsync(TowYard towYard, CancellationToken cancellationToken);
}
