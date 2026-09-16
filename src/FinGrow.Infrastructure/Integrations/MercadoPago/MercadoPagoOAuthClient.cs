namespace FinGrow.Infrastructure.Integrations.MercadoPago;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

internal sealed class MercadoPagoOAuthClient : IMercadoPagoOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoOAuthClient(HttpClient httpClient, IOptions<MercadoPagoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Uri BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["platform_id"] = "mp",
            ["state"] = state,
            ["redirect_uri"] = _options.RedirectUri,
        };

        return new Uri(QueryHelpers.AddQueryString(_options.AuthorizationUrl, query));
    }

    public Task<MercadoPagoTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new TokenRequest(_options.ClientId, _options.ClientSecret, "authorization_code")
        {
            Code = code,
            RedirectUri = _options.RedirectUri,
        }, cancellationToken);

    public Task<MercadoPagoTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new TokenRequest(_options.ClientId, _options.ClientSecret, "refresh_token")
        {
            RefreshToken = refreshToken,
        }, cancellationToken);

    private async Task<MercadoPagoTokens> RequestTokensAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("oauth/token", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new HttpRequestException("Mercado Pago devolvio una respuesta vacia al pedir el token.");

        if (string.IsNullOrEmpty(payload.AccessToken) || string.IsNullOrEmpty(payload.RefreshToken) || payload.UserId is null)
        {
            throw new HttpRequestException("Mercado Pago devolvio un token incompleto.");
        }

        return new MercadoPagoTokens(
            payload.AccessToken,
            payload.RefreshToken,
            TimeSpan.FromSeconds(payload.ExpiresIn),
            payload.UserId.Value.ToString(CultureInfo.InvariantCulture));
    }

    private sealed record TokenRequest(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("client_secret")] string ClientSecret,
        [property: JsonPropertyName("grant_type")] string GrantType)
    {
        [JsonPropertyName("code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }

        [JsonPropertyName("redirect_uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RedirectUri { get; init; }

        [JsonPropertyName("refresh_token")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; init; }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn,
        [property: JsonPropertyName("user_id")] long? UserId);
}
