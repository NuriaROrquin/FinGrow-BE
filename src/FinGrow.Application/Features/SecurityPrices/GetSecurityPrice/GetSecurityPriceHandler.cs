namespace FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;

using Common;
using Domain.Enums;
using Domain.Repositories;
using DTOs;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class GetSecurityPriceHandler(
    IMarketPriceProvider marketPrices,
    ISecurityPriceRepository storedPrices,
    IDateTimeProvider clock,
    ILogger<GetSecurityPriceHandler> logger) : IRequestHandler<GetSecurityPriceQuery, Result<SecurityPriceResponse>>
{
    internal static readonly TimeSpan LiveLookupTimeout = TimeSpan.FromSeconds(10);

    public async Task<Result<SecurityPriceResponse>> Handle(GetSecurityPriceQuery request, CancellationToken cancellationToken)
    {
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var (livePrice, sourceAnswered) = await FindLivePriceAsync(symbol, request.Currency, cancellationToken);

        if (livePrice is not null)
        {
            return Result.Success(new SecurityPriceResponse(
                livePrice.Symbol,
                livePrice.Currency,
                livePrice.UnitPrice,
                clock.Today,
                marketPrices.Source));
        }

        var stored = await storedPrices.FindAsync(symbol, request.Currency, cancellationToken);

        if (stored is not null)
        {
            return Result.Success(SecurityPriceResponse.FromStored(stored));
        }

        return Result.Failure<SecurityPriceResponse>(sourceAnswered
            ? SecurityPriceErrors.NotFound(symbol, request.Currency)
            : SecurityPriceErrors.Unavailable());
    }

    private async Task<(MarketPrice? Price, bool SourceAnswered)> FindLivePriceAsync(
        string symbol,
        Currency currency,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(LiveLookupTimeout);

        try
        {
            var prices = await marketPrices.GetClosingPricesAsync(timeout.Token);

            return (prices.FirstOrDefault(price => price.Symbol == symbol && price.Currency == currency), true);
        }
        catch (HttpRequestException exception)
        {
            LogSourceUnavailable(logger, exception, marketPrices.Source);

            return (null, false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSourceUnavailable(logger, exception, marketPrices.Source);

            return (null, false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudieron obtener los precios de {Source}; se busca el ultimo cierre guardado.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception, string source);
}
