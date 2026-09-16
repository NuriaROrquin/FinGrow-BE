namespace FinGrow.Infrastructure.Integrations.Telegram;

using System.Security.Cryptography;
using System.Text;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Options;

internal sealed class TelegramWebhookValidator : ITelegramWebhookValidator
{
    private readonly byte[] _secret;

    public TelegramWebhookValidator(IOptions<TelegramOptions> options) =>
        _secret = Encoding.UTF8.GetBytes(options.Value.WebhookSecret);

    public bool IsValid(string? secretToken) =>
        !string.IsNullOrEmpty(secretToken)
        && CryptographicOperations.FixedTimeEquals(_secret, Encoding.UTF8.GetBytes(secretToken));
}
