namespace FinGrow.Application.Interfaces;

public interface ITelegramBotClient
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
