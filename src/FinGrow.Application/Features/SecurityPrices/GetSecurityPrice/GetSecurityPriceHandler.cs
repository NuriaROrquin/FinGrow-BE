namespace FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;

using Common;
using Domain.Enums;
using Domain.Repositories;
using DTOs;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class GetSecurityPriceHandler(
    IEnumerable<IMarketPriceProvider> marketPrices,
    ISecurityPriceRepository storedPrices,
    IDateTimeProvider clock,
    ILogger<GetSecurityPriceHandler> logger) : IRequestHandler<GetSecurityPriceQuery, Result<SecurityPriceResponse>>
{
    internal static readonly TimeSpan LiveLookupTimeout = TimeSpan.FromSeconds(10);

    public async Task<Result<SecurityPriceResponse>> Handle(GetSecurityPriceQuery request, CancellationToken cancellationToken)
    {
        if (request.Type.QuotedOn() is not { } market)
        {
            return Result.Failure<SecurityPriceResponse>(SecurityPriceErrors.NotQuoted());
        }

        var symbol = market.NormalizeSymbol(request.Symbol);
        var provider = marketPrices.FirstOrDefault(candidate => candidate.Market == market);
        var (livePrice, sourceAnswered) = provider is null
            ? (null, false)
            : await FindLivePriceAsync(provider, symbol, request.Currency, cancellationToken);

        if (livePrice is not null)
        {
            return Result.Success(SecurityPriceResponse.FromMarketPrice(livePrice.Price, livePrice.Source, clock.Today));
        }

        var stored = await storedPrices.FindAsync(market, symbol, request.Currency, cancellationToken);

        if (stored is not null)
        {
            return Result.Success(SecurityPriceResponse.FromStored(stored));
        }

        return Result.Failure<SecurityPriceResponse>(sourceAnswered
            ? SecurityPriceErrors.NotFound(market, provider!.Source, symbol, request.Currency)
            : SecurityPriceErrors.Unavailable(provider?.Source ?? "la fuente de precios"));
    }

    private async Task<(IndexedPrice? Price, bool SourceAnswered)> FindLivePriceAsync(
        IMarketPriceProvider provider,
        string symbol,
        Currency currency,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(LiveLookupTimeout);

        try
        {
            var prices = await provider.GetClosingPricesAsync(timeout.Token);

            return (MarketPriceIndex.Of(provider, prices).Find(provider.Market, symbol, currency), true);
        }
        catch (HttpRequestException exception)
        {
            LogSourceUnavailable(logger, exception, provider.Source);

            return (null, false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSourceUnavailable(logger, exception, provider.Source);

            return (null, false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudieron obtener los precios de {Source}; se busca el ultimo cierre guardado.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception, string source);
}
