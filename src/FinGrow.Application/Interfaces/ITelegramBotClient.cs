namespace FinGrow.Application.Interfaces;

public sealed record TelegramButton(string Text, string CallbackData);

public interface ITelegramBotClient
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);

    Task SendMessageAsync(
        long chatId,
        string text,
        IReadOnlyList<IReadOnlyList<TelegramButton>> buttonRows,
        CancellationToken cancellationToken = default);

    Task EditMessageAsync(long chatId, long messageId, string text, CancellationToken cancellationToken = default);

    Task AnswerCallbackAsync(string callbackQueryId, string? text = null, CancellationToken cancellationToken = default);
}
