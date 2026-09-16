namespace FinGrow.Application.Features.Integrations.MercadoPago.CompleteMercadoPagoLink;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class CompleteMercadoPagoLinkHandler : IRequestHandler<CompleteMercadoPagoLinkCommand, Result>
{
    internal static readonly Error InvalidState = Error.Validation(
        "Integrations.MercadoPago.InvalidState",
        "La vinculacion vencio o no fue iniciada desde FinGrow. Volve a intentar.");

    private readonly LinkCodeRedeemer _linker;
    private readonly IMercadoPagoOAuthClient _oauth;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CompleteMercadoPagoLinkHandler> _logger;

    public CompleteMercadoPagoLinkHandler(
        LinkCodeRedeemer linker,
        IMercadoPagoOAuthClient oauth,
        IDateTimeProvider clock,
        ILogger<CompleteMercadoPagoLinkHandler> logger)
    {
        _linker = linker;
        _oauth = oauth;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteMercadoPagoLinkCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Error))
        {
            return Result.Failure(Error.Validation(
                "Integrations.MercadoPago.Denied", "Mercado Pago no autorizo la conexion con FinGrow."));
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
        {
            return Result.Failure(Error.Validation(
                "Integrations.MercadoPago.IncompleteCallback", "Mercado Pago no devolvio el codigo de autorizacion."));
        }

        if (!await _linker.IsRedeemableAsync(IntegrationProvider.MercadoPago, request.State, cancellationToken))
        {
            return Result.Failure(InvalidState);
        }

        MercadoPagoTokens tokens;

        try
        {
            tokens = await _oauth.ExchangeCodeAsync(request.Code, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogExchangeFailed(_logger, exception);

            return Result.Failure(Error.Failure(
                "Integrations.MercadoPago.ExchangeFailed", "Mercado Pago no entrego las credenciales. Volve a intentar."));
        }

        var grant = OAuthGrant.From(tokens.AccessToken, tokens.RefreshToken, _clock.UtcNow.Add(tokens.ExpiresIn));
        var attempt = await _linker.TryLinkAsync(
            IntegrationProvider.MercadoPago, tokens.UserId, request.State, cancellationToken, grant);

        return attempt.Outcome == LinkOutcome.Linked ? Result.Success() : Result.Failure(InvalidState);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo canjear el codigo de autorizacion de Mercado Pago.")]
    private static partial void LogExchangeFailed(ILogger logger, Exception exception);
}
