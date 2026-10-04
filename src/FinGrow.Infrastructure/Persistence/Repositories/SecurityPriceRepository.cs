namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class SecurityPriceRepository(FinGrowDbContext dbContext) : ISecurityPriceRepository
{
    public Task<SecurityPrice?> FindAsync(string symbol, Currency currency, CancellationToken cancellationToken = default) =>
        dbContext.SecurityPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(price => price.Symbol == symbol && price.Currency == currency, cancellationToken);

    public async Task<IReadOnlyList<SecurityPrice>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SecurityPrices.ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<SecurityPrice> prices) => dbContext.SecurityPrices.AddRange(prices);
}
