namespace FinGrow.Infrastructure.Integrations.Iol;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;

internal sealed class IolAuthClient : IIolAuthClient
{
    private readonly HttpClient _httpClient;

    public IolAuthClient(HttpClient httpClient) => _httpClient = httpClient;

    public Task<IolTokens> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        RequestTokensAsync("password", new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
            ["grant_type"] = "password",
        }, cancellationToken);

    public Task<IolTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokensAsync("refresh_token", new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }, cancellationToken);

    // Ni el cuerpo del pedido ni el de la respuesta van al mensaje de error: llevan la contraseña o los tokens.
    private async Task<IolTokens> RequestTokensAsync(
        string grantType,
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync("token", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"IOL respondio {(int)response.StatusCode} al pedir el token ({grantType}).",
                inner: null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new HttpRequestException("IOL devolvio una respuesta vacia al pedir el token.");

        if (string.IsNullOrEmpty(payload.AccessToken) || string.IsNullOrEmpty(payload.RefreshToken) || payload.ExpiresIn <= 0)
        {
            throw new HttpRequestException($"IOL devolvio un token incompleto ({grantType}).");
        }

        return new IolTokens(payload.AccessToken, payload.RefreshToken, Lifetime(payload));
    }

    // IOL informa expires_in = 1200, pero el bearer muere a los 15 minutos que marcan .issued y .expires.
    private static TimeSpan Lifetime(TokenResponse payload)
    {
        var declared = TimeSpan.FromSeconds(payload.ExpiresIn);

        if (!TryParseDate(payload.Issued, out var issued) || !TryParseDate(payload.Expires, out var expires) || expires <= issued)
        {
            return declared;
        }

        var actual = expires - issued;

        return actual < declared ? actual : declared;
    }

    private static bool TryParseDate(string? value, out DateTimeOffset date) =>
        DateTimeOffset.TryParseExact(value, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] long ExpiresIn,
        [property: JsonPropertyName(".issued")] string? Issued,
        [property: JsonPropertyName(".expires")] string? Expires);
}
