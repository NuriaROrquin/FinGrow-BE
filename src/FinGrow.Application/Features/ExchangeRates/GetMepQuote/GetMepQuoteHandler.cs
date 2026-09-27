namespace FinGrow.Application.Features.ExchangeRates.GetMepQuote;

using Common;
using DTOs;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class GetMepQuoteHandler(
    IExchangeRateProvider exchangeRateProvider,
    ILogger<GetMepQuoteHandler> logger) : IRequestHandler<GetMepQuoteQuery, Result<MepQuoteResponse>>
{
    public async Task<Result<MepQuoteResponse>> Handle(GetMepQuoteQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var quote = await exchangeRateProvider.GetMepQuoteAsync(cancellationToken);

            return Result.Success(MepQuoteResponse.FromQuote(quote));
        }
        catch (HttpRequestException exception)
        {
            LogMepUnavailable(logger, exception);

            return Result.Failure<MepQuoteResponse>(ExchangeRateErrors.MepUnavailable());
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogMepUnavailable(logger, exception);

            return Result.Failure<MepQuoteResponse>(ExchangeRateErrors.MepUnavailable());
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo obtener la cotizacion del dolar MEP.")]
    private static partial void LogMepUnavailable(ILogger logger, Exception exception);
}
