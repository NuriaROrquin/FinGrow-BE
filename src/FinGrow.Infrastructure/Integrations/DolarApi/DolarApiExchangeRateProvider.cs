namespace FinGrow.Infrastructure.Integrations.DolarApi;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

internal sealed class DolarApiExchangeRateProvider : IExchangeRateProvider
{
    internal const string MepPath = "v1/dolares/bolsa";
    internal const string Source = "DolarApi";
    private const string MepCacheKey = "exchange-rates:mep";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly DolarApiOptions _options;

    public DolarApiExchangeRateProvider(HttpClient httpClient, IMemoryCache cache, IOptions<DolarApiOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<MepQuote> GetMepQuoteAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(MepCacheKey, out MepQuote? cached) && cached is not null)
        {
            return cached;
        }

        var quote = await FetchMepQuoteAsync(cancellationToken);
        _cache.Set(MepCacheKey, quote, TimeSpan.FromMinutes(_options.CacheMinutes));

        return quote;
    }

    private async Task<MepQuote> FetchMepQuoteAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(new Uri(MepPath, UriKind.Relative), cancellationToken);

        response.EnsureSuccessStatusCode();

        DolarApiQuote? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<DolarApiQuote>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("DolarApi devolvio una cotizacion que no se pudo leer.", exception);
        }

        if (payload is null || payload.Buy <= 0m || payload.Sell <= 0m || payload.UpdatedAt == default)
        {
            throw new HttpRequestException("DolarApi devolvio una cotizacion incompleta.");
        }

        return new MepQuote(payload.Buy, payload.Sell, payload.UpdatedAt, Source);
    }

    private sealed record DolarApiQuote(
        [property: JsonPropertyName("compra")] decimal Buy,
        [property: JsonPropertyName("venta")] decimal Sell,
        [property: JsonPropertyName("fechaActualizacion")] DateTimeOffset UpdatedAt);
}
