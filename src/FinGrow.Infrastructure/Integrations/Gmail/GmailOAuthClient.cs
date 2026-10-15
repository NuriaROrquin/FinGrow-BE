namespace FinGrow.Infrastructure.Integrations.Gmail;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

internal sealed class GmailOAuthClient : IGmailOAuthClient
{
    internal const string ReadOnlyScope = "https://www.googleapis.com/auth/gmail.readonly";

    private readonly HttpClient _httpClient;
    private readonly GmailOptions _options;

    public GmailOAuthClient(HttpClient httpClient, IOptions<GmailOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    // access_type=offline pide el refresh token y prompt=consent lo fuerza tambien al revincular:
    // sin el, Google solo lo entrega la primera vez que la cuenta autoriza a la app.
    public Uri BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = ReadOnlyScope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state,
        };

        return new Uri(QueryHelpers.AddQueryString(_options.AuthorizationUrl, query));
    }

    public Task<GmailTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
        RequestTokensAsync("authorization_code", new Dictionary<string, string>
        {
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        }, cancellationToken);

    public Task<GmailTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokensAsync("refresh_token", new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    public async Task<string> GetEmailAddressAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri(new Uri(_options.ApiBaseUrl), "gmail/v1/users/me/profile"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "al pedir el perfil de Gmail", cancellationToken);

        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>(cancellationToken);

        return string.IsNullOrWhiteSpace(profile?.EmailAddress)
            ? throw new HttpRequestException("Gmail devolvio un perfil sin direccion de mail.")
            : profile.EmailAddress;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
        using var response = await _httpClient.PostAsync("revoke", content, cancellationToken);

        // Google responde 400 cuando el token ya no es valido: el empleado lo revoco desde su cuenta
        // o vencio, y en los dos casos el acceso ya no existe.
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return;
        }

        await EnsureSuccessAsync(response, "al revocar el acceso", cancellationToken);
    }

    private async Task<GmailTokens> RequestTokensAsync(
        string grantType,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        parameters["client_id"] = _options.ClientId;
        parameters["client_secret"] = _options.ClientSecret;
        parameters["grant_type"] = grantType;

        using var content = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync("token", content, cancellationToken);
        await EnsureSuccessAsync(response, $"al pedir el token ({grantType})", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new HttpRequestException("Google devolvio una respuesta vacia al pedir el token.");

        if (string.IsNullOrEmpty(payload.AccessToken))
        {
            throw new HttpRequestException($"Google devolvio un token sin access_token (scope '{payload.Scope}').");
        }

        var scopes = (payload.Scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new GmailTokens(
            payload.AccessToken,
            string.IsNullOrWhiteSpace(payload.RefreshToken) ? null : payload.RefreshToken,
            TimeSpan.FromSeconds(payload.ExpiresIn),
            scopes.Contains(ReadOnlyScope, StringComparer.Ordinal));
    }

    internal static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new HttpRequestException(
            $"Google respondio {(int)response.StatusCode} {operation}: {body}",
            inner: null,
            response.StatusCode);
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope);

    private sealed record ProfileResponse([property: JsonPropertyName("emailAddress")] string? EmailAddress);
}
