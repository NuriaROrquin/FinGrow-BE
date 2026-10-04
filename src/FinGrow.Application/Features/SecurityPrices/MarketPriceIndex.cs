namespace FinGrow.Application.Features.SecurityPrices;

using Domain.Enums;
using Interfaces;

internal sealed record IndexedPrice(PriceMarket Market, string Source, MarketPrice Price);

internal sealed class MarketPriceIndex
{
    private readonly Dictionary<(PriceMarket Market, string Key, Currency Currency), IndexedPrice> _byCurrency = new();
    private readonly Dictionary<(PriceMarket Market, string Key), IndexedPrice> _bySymbol = new();

    public IEnumerable<IndexedPrice> Prices => _byCurrency.Values;

    public static MarketPriceIndex Of(IMarketPriceProvider provider, IEnumerable<MarketPrice> prices)
    {
        var index = new MarketPriceIndex();
        index.Add(provider, prices);

        return index;
    }

    public void Add(IMarketPriceProvider provider, IEnumerable<MarketPrice> prices)
    {
        foreach (var price in prices.Where(price => !string.IsNullOrWhiteSpace(price.Symbol)))
        {
            var indexed = new IndexedPrice(provider.Market, provider.Source, price);
            var key = KeyOf(provider.Market, price.Symbol);

            _byCurrency.TryAdd((provider.Market, key, price.Currency), indexed);
            _bySymbol.TryAdd((provider.Market, key), indexed);
        }
    }

    public IndexedPrice? Find(PriceMarket market, string symbol, Currency currency)
    {
        var key = KeyOf(market, symbol);

        return market.IgnoresCurrency()
            ? _bySymbol.GetValueOrDefault((market, key))
            : _byCurrency.GetValueOrDefault((market, key, currency));
    }

    internal static string KeyOf(PriceMarket market, string symbol) =>
        market.NormalizeSymbol(symbol).ToUpperInvariant();
}
