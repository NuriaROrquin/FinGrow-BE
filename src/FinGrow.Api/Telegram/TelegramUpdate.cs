namespace FinGrow.Api.Telegram;

using System.Text.Json.Serialization;

public sealed record TelegramUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessage? Message,
    [property: JsonPropertyName("callback_query")] TelegramCallbackQuery? CallbackQuery = null);

public sealed record TelegramMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("chat")] TelegramChat Chat,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("voice")] TelegramVoice? Voice = null,
    [property: JsonPropertyName("photo")] IReadOnlyList<TelegramPhotoSize>? Photo = null,
    [property: JsonPropertyName("document")] TelegramDocument? Document = null);

public sealed record TelegramVoice(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("mime_type")] string? MimeType);

public sealed record TelegramPhotoSize(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramDocument(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("file_name")] string? FileName = null);

public sealed record TelegramChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type)
{
    public const string Private = "private";

    public bool IsPrivate => string.Equals(Type, Private, StringComparison.OrdinalIgnoreCase);
}

public sealed record TelegramCallbackQuery(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("from")] TelegramUser From,
    [property: JsonPropertyName("message")] TelegramMessage? Message,
    [property: JsonPropertyName("data")] string? Data);

public sealed record TelegramUser(
    [property: JsonPropertyName("id")] long Id);
