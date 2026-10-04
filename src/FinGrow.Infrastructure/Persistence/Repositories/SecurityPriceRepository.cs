namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class SecurityPriceRepository(FinGrowDbContext dbContext) : ISecurityPriceRepository
{
    public Task<SecurityPrice?> FindAsync(
        PriceMarket market,
        string symbol,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        var prices = dbContext.SecurityPrices
            .AsNoTracking()
            .Where(price => price.Market == market && price.Symbol == symbol);

        if (!market.IgnoresCurrency())
        {
            prices = prices.Where(price => price.Currency == currency);
        }

        return prices
            .OrderByDescending(price => price.PricedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityPrice>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SecurityPrices.ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<SecurityPrice> prices) => dbContext.SecurityPrices.AddRange(prices);
}
