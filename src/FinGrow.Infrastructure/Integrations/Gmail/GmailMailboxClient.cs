namespace FinGrow.Infrastructure.Integrations.Gmail;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class GmailMailboxClient : IGmailMailboxClient
{
    private const int PageSize = 100;

    private readonly HttpClient _httpClient;

    public GmailMailboxClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<GmailMessagePage> SearchAsync(
        string accessToken,
        string query,
        string? pageToken,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["q"] = query,
            ["maxResults"] = PageSize.ToString(CultureInfo.InvariantCulture),
        };

        if (pageToken is not null)
        {
            parameters["pageToken"] = pageToken;
        }

        var page = await GetAsync<MessageListResponse>(
            accessToken,
            QueryHelpers.AddQueryString("gmail/v1/users/me/messages", parameters),
            "al buscar mails",
            cancellationToken);

        var ids = (page.Messages ?? new List<MessageReference>())
            .Select(message => message.Id)
            .OfType<string>()
            .ToList();

        return new GmailMessagePage(ids, string.IsNullOrEmpty(page.NextPageToken) ? null : page.NextPageToken);
    }

    public async Task<GmailMessage> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await GetAsync<MessageResponse>(
            accessToken,
            $"gmail/v1/users/me/messages/{Uri.EscapeDataString(messageId)}?format=full",
            "al leer un mail",
            cancellationToken);

        var receivedAt = long.TryParse(message.InternalDate, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
            : throw new HttpRequestException($"Gmail devolvio el mail {messageId} sin fecha.");

        return new GmailMessage(
            messageId,
            GmailMessageText.Header(message.Payload, "Subject"),
            GmailMessageText.SenderDomain(GmailMessageText.Header(message.Payload, "From")),
            receivedAt,
            GmailMessageText.Extract(message.Payload));
    }

    private async Task<T> GetAsync<T>(string accessToken, string path, string operation, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await GmailOAuthClient.EnsureSuccessAsync(response, operation, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new HttpRequestException($"Gmail devolvio una respuesta vacia {operation}.");
    }

    private sealed record MessageListResponse(
        [property: JsonPropertyName("messages")] List<MessageReference>? Messages,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);

    private sealed record MessageReference([property: JsonPropertyName("id")] string? Id);

    private sealed record MessageResponse(
        [property: JsonPropertyName("internalDate")] string? InternalDate,
        [property: JsonPropertyName("payload")] GmailPayload? Payload);
}

internal sealed record GmailPayload(
    [property: JsonPropertyName("mimeType")] string? MimeType,
    [property: JsonPropertyName("filename")] string? Filename,
    [property: JsonPropertyName("headers")] List<GmailHeader>? Headers,
    [property: JsonPropertyName("body")] GmailBody? Body,
    [property: JsonPropertyName("parts")] List<GmailPayload>? Parts);

internal sealed record GmailHeader(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("value")] string? Value);

internal sealed record GmailBody([property: JsonPropertyName("data")] string? Data);
