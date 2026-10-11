namespace FinGrow.Application.Features.Integrations.Gmail.CompleteGmailLink;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class CompleteGmailLinkHandler : IRequestHandler<CompleteGmailLinkCommand, Result>
{
    internal static readonly Error InvalidState = Error.Validation(
        "Integrations.Gmail.InvalidState",
        "La vinculacion vencio o no fue iniciada desde FinGrow. Volve a intentar.");

    internal static readonly Error MailAccessNotGranted = Error.Validation(
        "Integrations.Gmail.MailAccessNotGranted",
        "Para registrar tus consumos FinGrow necesita permiso para leer tus mails. Volve a intentar y marca esa opcion.");

    internal static readonly Error ExchangeFailed = Error.Failure(
        "Integrations.Gmail.ExchangeFailed",
        "Google no entrego las credenciales. Volve a intentar.");

    private readonly LinkCodeRedeemer _linker;
    private readonly IGmailOAuthClient _oauth;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CompleteGmailLinkHandler> _logger;

    public CompleteGmailLinkHandler(
        LinkCodeRedeemer linker,
        IGmailOAuthClient oauth,
        IDateTimeProvider clock,
        ILogger<CompleteGmailLinkHandler> logger)
    {
        _linker = linker;
        _oauth = oauth;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteGmailLinkCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Error))
        {
            return Result.Failure(Error.Validation(
                "Integrations.Gmail.Denied", "Google no autorizo la conexion con FinGrow."));
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
        {
            return Result.Failure(Error.Validation(
                "Integrations.Gmail.IncompleteCallback", "Google no devolvio el codigo de autorizacion."));
        }

        if (!await _linker.IsRedeemableAsync(IntegrationProvider.Gmail, request.State, cancellationToken))
        {
            return Result.Failure(InvalidState);
        }

        GmailTokens tokens;
        string emailAddress;

        try
        {
            tokens = await _oauth.ExchangeCodeAsync(request.Code, cancellationToken);

            // Google deja destildar cada permiso en la pantalla de consentimiento: sin el de lectura
            // la vinculacion quedaria activa pero nunca podria sincronizar nada.
            if (!tokens.CanReadMail)
            {
                return Result.Failure(MailAccessNotGranted);
            }

            // Sin refresh token no hay forma de leer mails cuando el empleado no esta en la app.
            if (tokens.RefreshToken is null)
            {
                return Result.Failure(ExchangeFailed);
            }

            emailAddress = await _oauth.GetEmailAddressAsync(tokens.AccessToken, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogExchangeFailed(_logger, exception);

            return Result.Failure(ExchangeFailed);
        }

        var grant = OAuthGrant.From(tokens.AccessToken, tokens.RefreshToken, _clock.UtcNow.Add(tokens.ExpiresIn));
        var attempt = await _linker.TryLinkAsync(
            IntegrationProvider.Gmail, emailAddress, request.State, cancellationToken, grant);

        return attempt.Outcome == LinkOutcome.Linked ? Result.Success() : Result.Failure(InvalidState);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo canjear el codigo de autorizacion de Google.")]
    private static partial void LogExchangeFailed(ILogger logger, Exception exception);
}
