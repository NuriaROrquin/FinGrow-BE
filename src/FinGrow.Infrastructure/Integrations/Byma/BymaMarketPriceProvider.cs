namespace FinGrow.Infrastructure.Integrations.Byma;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Options;

internal sealed class BymaMarketPriceProvider : IMarketPriceProvider
{
    internal const string NextDaySettlementCode = "2";

    internal static readonly IReadOnlyList<BymaPanel> Panels = new[]
    {
        new BymaPanel("leading-equity", 1m),
        new BymaPanel("general-equity", 1m),
        new BymaPanel("cedears", 1m),
        new BymaPanel("public-bonds", 100m),
        new BymaPanel("negociable-obligations", 100m),
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly BymaOptions _options;

    public BymaMarketPriceProvider(HttpClient httpClient, IOptions<BymaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Source => "BYMA";

    public async Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default)
    {
        var prices = new List<MarketPrice>();

        foreach (var panel in Panels)
        {
            var pageNumber = 1;
            int pageCount;

            do
            {
                var page = await FetchPageAsync(panel.Name, pageNumber, cancellationToken);

                prices.AddRange(page.Rows
                    .Select(row => ToMarketPrice(row, panel.PricedPer))
                    .OfType<MarketPrice>());

                pageCount = page.PageCount;
                pageNumber++;
            }
            while (pageNumber <= pageCount && pageNumber <= _options.MaxPagesPerPanel);
        }

        return prices;
    }

    private async Task<BymaPage> FetchPageAsync(string panel, int pageNumber, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(panel, UriKind.Relative),
            new BymaPanelRequest(pageNumber),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return new BymaPage(root.Deserialize<List<BymaRow>>(JsonOptions) ?? new List<BymaRow>(), 1);
            }

            var rows = root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
                ? data.Deserialize<List<BymaRow>>(JsonOptions) ?? new List<BymaRow>()
                : new List<BymaRow>();

            var pageCount = root.TryGetProperty("content", out var content)
                && content.TryGetProperty("page_count", out var count)
                && count.TryGetInt32(out var value)
                    ? value
                    : 1;

            return new BymaPage(rows, pageCount);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException($"BYMA devolvio el panel {panel} con un formato que no se pudo leer.", exception);
        }
    }

    private static MarketPrice? ToMarketPrice(BymaRow row, decimal pricedPer)
    {
        if (string.IsNullOrWhiteSpace(row.Symbol) || row.SettlementType != NextDaySettlementCode)
        {
            return null;
        }

        Currency? currency = row.DenominationCcy switch
        {
            "ARS" => Currency.ARS,
            "USD" => Currency.USD,
            _ => null,
        };

        var price = row.ClosingPrice is > 0m ? row.ClosingPrice.Value : row.PreviousClosingPrice ?? 0m;

        return currency is null || price <= 0m
            ? null
            : new MarketPrice(row.Symbol.Trim().ToUpperInvariant(), currency.Value, price / pricedPer);
    }

    internal sealed record BymaPanel(string Name, decimal PricedPer);

    private sealed record BymaPage(IReadOnlyList<BymaRow> Rows, int PageCount);

    private sealed record BymaPanelRequest(
        [property: JsonPropertyName("page_number")] int PageNumber,
        [property: JsonPropertyName("excludeZeroPxAndQty")] bool ExcludeZeroPriceAndQuantity = false,
        [property: JsonPropertyName("T0")] bool SameDaySettlement = false,
        [property: JsonPropertyName("T1")] bool NextDaySettlement = true,
        [property: JsonPropertyName("T2")] bool TwoDaySettlement = false);

    private sealed record BymaRow(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("settlementType")] string? SettlementType,
        [property: JsonPropertyName("denominationCcy")] string? DenominationCcy,
        [property: JsonPropertyName("closingPrice")] decimal? ClosingPrice,
        [property: JsonPropertyName("previousClosingPrice")] decimal? PreviousClosingPrice);
}
