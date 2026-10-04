namespace FinGrow.Infrastructure.Integrations.CoinGecko;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

internal sealed partial class CoinGeckoCryptoPriceProvider : IMarketPriceProvider
{
    private const string CryptoPricesCacheKey = "market-prices:coingecko";

    internal static readonly IReadOnlyList<(string VsCurrency, Currency Currency)> QuoteCurrencies = new[]
    {
        ("usd", Currency.USD),
        ("ars", Currency.ARS),
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly CoinGeckoOptions _options;

    public CoinGeckoCryptoPriceProvider(HttpClient httpClient, IMemoryCache cache, IOptions<CoinGeckoOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public PriceMarket Market => PriceMarket.Crypto;

    public string Source => "CoinGecko";

    public async Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CryptoPricesCacheKey, out IReadOnlyList<MarketPrice>? cached) && cached is not null)
        {
            return cached;
        }

        var prices = new List<MarketPrice>();

        foreach (var (vsCurrency, currency) in QuoteCurrencies)
        {
            prices.AddRange(await FetchMarketsAsync(vsCurrency, currency, cancellationToken));
        }

        _cache.Set(CryptoPricesCacheKey, (IReadOnlyList<MarketPrice>)prices, TimeSpan.FromMinutes(_options.CacheMinutes));

        return prices;
    }

    private async Task<IReadOnlyList<MarketPrice>> FetchMarketsAsync(
        string vsCurrency,
        Currency currency,
        CancellationToken cancellationToken)
    {
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"coins/markets?vs_currency={vsCurrency}&order=market_cap_desc&per_page={_options.TopCoins}&page=1");

        using var response = await _httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        response.EnsureSuccessStatusCode();

        List<CoinRow>? coins;

        try
        {
            coins = await response.Content.ReadFromJsonAsync<List<CoinRow>>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException($"CoinGecko devolvio las cotizaciones en {vsCurrency} con un formato que no se pudo leer.", exception);
        }

        return (coins ?? new List<CoinRow>())
            .Where(coin => coin.CurrentPrice is > 0m && !string.IsNullOrWhiteSpace(coin.Symbol))
            .Select(coin => (Coin: coin, Symbol: coin.Symbol!.Trim().ToUpperInvariant()))
            .Where(coin => TickerPattern().IsMatch(coin.Symbol))
            .DistinctBy(coin => coin.Symbol)
            .Select(coin => new MarketPrice(coin.Symbol, currency, coin.Coin.CurrentPrice!.Value, coin.Coin.Name?.Trim()))
            .ToList();
    }

    [GeneratedRegex("^[A-Z0-9]{1,20}$")]
    private static partial Regex TickerPattern();

    private sealed record CoinRow(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("current_price")] decimal? CurrentPrice);
}
