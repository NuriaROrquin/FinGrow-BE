namespace FinGrow.Infrastructure.Integrations.Telegram;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;

internal sealed class TelegramBotClient : ITelegramBotClient
{
    private readonly HttpClient _httpClient;

    public TelegramBotClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "sendMessage", new SendMessageRequest(chatId, text), cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text);
}
