namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface ISecurityPriceRepository
{
    Task<SecurityPrice?> FindAsync(string symbol, Currency currency, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecurityPrice>> ListAsync(CancellationToken cancellationToken = default);

    void AddRange(IEnumerable<SecurityPrice> prices);
}
