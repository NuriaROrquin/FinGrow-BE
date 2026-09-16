namespace FinGrow.Api.Telegram;

using System.Text.Json.Serialization;

public sealed record TelegramUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessage? Message);

public sealed record TelegramMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("chat")] TelegramChat Chat,
    [property: JsonPropertyName("text")] string? Text);

public sealed record TelegramChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type)
{
    public const string Private = "private";

    public bool IsPrivate => string.Equals(Type, Private, StringComparison.OrdinalIgnoreCase);
}
