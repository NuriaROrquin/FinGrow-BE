namespace FinGrow.Infrastructure.Integrations;

using FinGrow.Application.Interfaces;

internal sealed class DisabledTwilioRequestValidator : ITwilioRequestValidator
{
    public bool IsValid(string url, IReadOnlyDictionary<string, string> form, string? signature) => false;
}

internal sealed class DisabledTelegramWebhookValidator : ITelegramWebhookValidator
{
    public bool IsValid(string? secretToken) => false;
}

internal sealed class DisabledTwilioMediaClient : ITwilioMediaClient
{
    public Task<TwilioMedia> DownloadAsync(Uri url, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("La integracion de Twilio esta desactivada para este entorno.");
}

internal sealed class DisabledTelegramBotClient : ITelegramBotClient
{
    public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("La integracion de Telegram esta desactivada para este entorno.");
}

internal sealed class DisabledMercadoPagoOAuthClient : IMercadoPagoOAuthClient
{
    public Uri BuildAuthorizationUrl(string state) =>
        throw new InvalidOperationException("La integracion de Mercado Pago esta desactivada para este entorno.");

    public Task<MercadoPagoTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("La integracion de Mercado Pago esta desactivada para este entorno.");

    public Task<MercadoPagoTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("La integracion de Mercado Pago esta desactivada para este entorno.");
}

internal sealed class DisabledMercadoPagoPaymentsClient : IMercadoPagoPaymentsClient
{
    public Task<MercadoPagoPaymentsPage> SearchUpdatedBetweenAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to,
        int offset,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("La integracion de Mercado Pago esta desactivada para este entorno.");
}