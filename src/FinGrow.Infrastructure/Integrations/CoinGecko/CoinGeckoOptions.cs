namespace FinGrow.Infrastructure.Integrations.CoinGecko;

using System.ComponentModel.DataAnnotations;

public sealed class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";

    public const string HttpClientName = "CoinGecko";

    public const string ApiKeyHeader = "x-cg-demo-api-key";

    [Required(ErrorMessage = "Falta configurar la URL base de CoinGecko (CoinGecko:BaseUrl).")]
    [Url]
    public string BaseUrl { get; init; } = "https://api.coingecko.com/api/v3/";

    public string? ApiKey { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 15;

    [Range(0, 1440)]
    public int CacheMinutes { get; init; } = 10;

    [Range(1, 250)]
    public int TopCoins { get; init; } = 250;
}
