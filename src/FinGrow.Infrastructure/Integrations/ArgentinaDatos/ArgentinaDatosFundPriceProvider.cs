namespace FinGrow.Infrastructure.Integrations.ArgentinaDatos;

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

internal sealed class ArgentinaDatosFundPriceProvider : IMarketPriceProvider
{
    internal const decimal CuotapartesPerQuote = 1000m;
    private const string FundPricesCacheKey = "market-prices:argentinadatos";

    internal static readonly IReadOnlyList<string> Categories = new[]
    {
        "mercadoDinero",
        "rentaFija",
        "rentaMixta",
        "rentaVariable",
        "retornoTotal",
    };

    private static readonly string[] DollarMarkers = { "DOLAR", "DOLLAR", "USD", "U$S" };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ArgentinaDatosOptions _options;

    public ArgentinaDatosFundPriceProvider(HttpClient httpClient, IMemoryCache cache, IOptions<ArgentinaDatosOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public PriceMarket Market => PriceMarket.MutualFund;

    public string Source => "ArgentinaDatos";

    public async Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(FundPricesCacheKey, out IReadOnlyList<MarketPrice>? cached) && cached is not null)
        {
            return cached;
        }

        var prices = await FetchFundPricesAsync(cancellationToken);
        _cache.Set(FundPricesCacheKey, prices, TimeSpan.FromMinutes(_options.CacheMinutes));

        return prices;
    }

    internal static Currency CurrencyOf(string fund)
    {
        var text = WithoutAccents(fund).ToUpperInvariant();
        var isDollar = DollarMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal))
            && !text.Contains("LINKED", StringComparison.Ordinal);

        return isDollar ? Currency.USD : Currency.ARS;
    }

    private async Task<IReadOnlyList<MarketPrice>> FetchFundPricesAsync(CancellationToken cancellationToken)
    {
        var rows = new List<FundRow>();

        foreach (var category in Categories)
        {
            rows.AddRange(await FetchCategoryAsync(category, cancellationToken));
        }

        var quoted = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.Fund) && row.Date is not null && row.QuoteValue is > 0m)
            .ToList();

        if (quoted.Count == 0)
        {
            return Array.Empty<MarketPrice>();
        }

        var oldestAccepted = quoted.Max(row => row.Date!.Value).AddDays(-_options.StaleAfterDays);

        return quoted
            .Where(row => row.Date!.Value >= oldestAccepted)
            .GroupBy(row => row.Fund!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(row => row.Date).First())
            .Select(row => new MarketPrice(
                row.Fund!.Trim(),
                CurrencyOf(row.Fund),
                row.QuoteValue!.Value / CuotapartesPerQuote,
                PricedOn: row.Date))
            .ToList();
    }

    private async Task<IReadOnlyList<FundRow>> FetchCategoryAsync(string category, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            new Uri($"v1/finanzas/fci/{category}/ultimo", UriKind.Relative),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<List<FundRow>>(cancellationToken) ?? new List<FundRow>();
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException($"ArgentinaDatos devolvio los fondos de {category} con un formato que no se pudo leer.", exception);
        }
    }

    private static string WithoutAccents(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed.Where(character =>
                     CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
        {
            builder.Append(character);
        }

        return builder.ToString();
    }

    private sealed record FundRow(
        [property: JsonPropertyName("fondo")] string? Fund,
        [property: JsonPropertyName("fecha")] DateOnly? Date,
        [property: JsonPropertyName("vcp")] decimal? QuoteValue);
}
