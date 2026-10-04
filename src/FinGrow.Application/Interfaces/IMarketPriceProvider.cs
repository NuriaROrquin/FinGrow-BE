namespace FinGrow.Application.Interfaces;

using Domain.Enums;

public sealed record MarketPrice(
    string Symbol,
    Currency Currency,
    decimal UnitPrice,
    string? Name = null,
    DateOnly? PricedOn = null);

public interface IMarketPriceProvider
{
    PriceMarket Market { get; }

    string Source { get; }

    Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default);
}
