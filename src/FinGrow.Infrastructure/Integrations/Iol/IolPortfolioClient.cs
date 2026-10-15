namespace FinGrow.Infrastructure.Integrations.Iol;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class IolPortfolioClient : IIolPortfolioClient
{
    private readonly HttpClient _httpClient;

    public IolPortfolioClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<IolPosition>> GetPortfolioAsync(
        string accessToken,
        IolCountry country,
        CancellationToken cancellationToken = default)
    {
        var payload = await GetAsync<PortfolioResponse>($"api/v2/portafolio/{CountryPath(country)}", accessToken, cancellationToken);

        return (payload.Assets ?? new List<Asset>())
            .Where(asset => asset.Title is not null)
            .Select(asset => new IolPosition(
                asset.Title!.Symbol ?? string.Empty,
                asset.Title.Description,
                asset.Title.Type ?? string.Empty,
                asset.Title.Market ?? string.Empty,
                asset.Title.Currency ?? string.Empty,
                asset.Quantity,
                asset.AveragePurchasePrice,
                asset.ValuedAmount))
            .ToList();
    }

    public async Task<IReadOnlyList<IolOperation>> ListCompletedOperationsAsync(
        string accessToken,
        DateOnly from,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["filtro.estado"] = "terminadas",
            ["filtro.fechaDesde"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };

        var operations = await GetAsync<List<Operation>>(
            QueryHelpers.AddQueryString("api/v2/operaciones", query),
            accessToken,
            cancellationToken);

        return operations
            .Select(operation => new IolOperation(
                operation.Symbol ?? string.Empty,
                operation.Type ?? string.Empty,
                DateOf(operation.ExecutedAt ?? operation.OrderedAt)))
            .ToList();
    }

    private async Task<T> GetAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new HttpRequestException($"IOL devolvio una respuesta vacia en {path}.");
    }

    private static string CountryPath(IolCountry country) => country switch
    {
        IolCountry.Argentina => "argentina",
        IolCountry.UnitedStates => "estados_Unidos",
        _ => throw new ArgumentOutOfRangeException(nameof(country), country, null),
    };

    // IOL manda la hora de Argentina sin zona ("2026-10-13T11:05:12.123"): alcanza con el dia, sin convertir.
    private static DateOnly? DateOf(string? value) =>
        value is { Length: >= 10 }
        && DateOnly.TryParseExact(value[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private sealed record PortfolioResponse([property: JsonPropertyName("activos")] List<Asset>? Assets);

    private sealed record Asset(
        [property: JsonPropertyName("cantidad"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Quantity,
        [property: JsonPropertyName("ppc"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal AveragePurchasePrice,
        [property: JsonPropertyName("valorizado"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal ValuedAmount,
        [property: JsonPropertyName("titulo")] Title? Title);

    private sealed record Title(
        [property: JsonPropertyName("simbolo")] string? Symbol,
        [property: JsonPropertyName("descripcion")] string? Description,
        [property: JsonPropertyName("tipo")] string? Type,
        [property: JsonPropertyName("mercado")] string? Market,
        [property: JsonPropertyName("moneda")] string? Currency);

    private sealed record Operation(
        [property: JsonPropertyName("simbolo")] string? Symbol,
        [property: JsonPropertyName("tipo")] string? Type,
        [property: JsonPropertyName("fechaOrden")] string? OrderedAt,
        [property: JsonPropertyName("fechaOperada")] string? ExecutedAt);
}
