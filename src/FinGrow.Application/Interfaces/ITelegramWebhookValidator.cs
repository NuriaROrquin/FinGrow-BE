namespace FinGrow.Application.Interfaces;

public interface ITelegramWebhookValidator
{
    bool IsValid(string? secretToken);
}
