namespace FinGrow.Infrastructure.Integrations.Telegram;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;

internal sealed class TelegramBotClient : ITelegramBotClient
{
    private readonly HttpClient _httpClient;

    public TelegramBotClient(HttpClient httpClient) => _httpClient = httpClient;

    public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        PostAsync("sendMessage", new SendMessageRequest(chatId, text, ReplyMarkup: null), cancellationToken);

    public Task SendMessageAsync(
        long chatId,
        string text,
        IReadOnlyList<IReadOnlyList<TelegramButton>> buttonRows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buttonRows);

        var keyboard = new InlineKeyboardMarkup(buttonRows
            .Select(row => (IReadOnlyList<InlineKeyboardButton>)row
                .Select(button => new InlineKeyboardButton(button.Text, button.CallbackData))
                .ToList())
            .ToList());

        return PostAsync("sendMessage", new SendMessageRequest(chatId, text, keyboard), cancellationToken);
    }

    public Task EditMessageAsync(long chatId, long messageId, string text, CancellationToken cancellationToken = default) =>
        PostAsync("editMessageText", new EditMessageTextRequest(chatId, messageId, text), cancellationToken);

    public Task AnswerCallbackAsync(string callbackQueryId, string? text = null, CancellationToken cancellationToken = default) =>
        PostAsync("answerCallbackQuery", new AnswerCallbackQueryRequest(callbackQueryId, text), cancellationToken);

    private async Task PostAsync<TRequest>(string method, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(method, request, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("reply_markup"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InlineKeyboardMarkup? ReplyMarkup);

    private sealed record InlineKeyboardMarkup(
        [property: JsonPropertyName("inline_keyboard")] IReadOnlyList<IReadOnlyList<InlineKeyboardButton>> InlineKeyboard);

    private sealed record InlineKeyboardButton(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("callback_data")] string CallbackData);

    private sealed record EditMessageTextRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("message_id")] long MessageId,
        [property: JsonPropertyName("text")] string Text);

    private sealed record AnswerCallbackQueryRequest(
        [property: JsonPropertyName("callback_query_id")] string CallbackQueryId,
        [property: JsonPropertyName("text"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text);
}
