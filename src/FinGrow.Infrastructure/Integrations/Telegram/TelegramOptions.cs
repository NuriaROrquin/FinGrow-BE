namespace FinGrow.Infrastructure.Integrations.Telegram;

using System.ComponentModel.DataAnnotations;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required(ErrorMessage = "Falta configurar el token del bot de Telegram (Telegram:BotToken).")]
    public string BotToken { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el secreto del webhook de Telegram (Telegram:WebhookSecret).")]
    [RegularExpression("^[A-Za-z0-9_-]{1,256}$",
        ErrorMessage = "Telegram:WebhookSecret admite hasta 256 caracteres: letras, numeros, '_' y '-'.")]
    public string WebhookSecret { get; init; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}
