namespace FinGrow.Application.Features.SecurityPrices.SearchSecurityPrices;

using Common;
using Domain.Enums;
using DTOs;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class SearchSecurityPricesHandler(
    IEnumerable<IMarketPriceProvider> marketPrices,
    IDateTimeProvider clock,
    ILogger<SearchSecurityPricesHandler> logger)
    : IRequestHandler<SearchSecurityPricesQuery, Result<IReadOnlyList<SecurityPriceResponse>>>
{
    public const int MaxResults = 20;

    public async Task<Result<IReadOnlyList<SecurityPriceResponse>>> Handle(
        SearchSecurityPricesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Type.QuotedOn() is not { } market)
        {
            return Result.Failure<IReadOnlyList<SecurityPriceResponse>>(SecurityPriceErrors.NotQuoted());
        }

        var provider = marketPrices.FirstOrDefault(candidate => candidate.Market == market);

        if (provider is null)
        {
            return Result.Failure<IReadOnlyList<SecurityPriceResponse>>(SecurityPriceErrors.Unavailable("la fuente de precios"));
        }

        IReadOnlyList<MarketPrice> prices;

        try
        {
            prices = await provider.GetClosingPricesAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return SourceUnavailable(provider, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SourceUnavailable(provider, exception);
        }

        var query = SearchText.Normalize(request.Query);
        var today = clock.Today;

        IReadOnlyList<SecurityPriceResponse> matches = prices
            .Where(price => request.Currency is null || market.IgnoresCurrency() || price.Currency == request.Currency)
            .Select(price => (Price: price, Symbol: SearchText.Normalize(price.Symbol), Name: SearchText.Normalize(price.Name)))
            .Where(candidate =>
                candidate.Symbol.Contains(query, StringComparison.Ordinal)
                || candidate.Name.Contains(query, StringComparison.Ordinal))
            .OrderBy(candidate =>
                candidate.Symbol.StartsWith(query, StringComparison.Ordinal)
                || candidate.Name.StartsWith(query, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(candidate => candidate.Symbol.Length)
            .ThenBy(candidate => candidate.Symbol, StringComparer.Ordinal)
            .Take(MaxResults)
            .Select(candidate => SecurityPriceResponse.FromMarketPrice(candidate.Price, provider.Source, today))
            .ToList();

        return Result.Success(matches);
    }

    private Result<IReadOnlyList<SecurityPriceResponse>> SourceUnavailable(IMarketPriceProvider provider, Exception exception)
    {
        LogSourceUnavailable(logger, exception, provider.Source);

        return Result.Failure<IReadOnlyList<SecurityPriceResponse>>(SecurityPriceErrors.Unavailable(provider.Source));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo buscar en los precios de {Source}.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception, string source);
}
