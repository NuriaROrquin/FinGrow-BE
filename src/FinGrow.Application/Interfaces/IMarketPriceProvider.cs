namespace FinGrow.Application.Interfaces;

using Domain.Enums;

public sealed record MarketPrice(string Symbol, Currency Currency, decimal UnitPrice);

public interface IMarketPriceProvider
{
    string Source { get; }

    Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default);
}
