namespace FinGrow.Application.Interfaces;

public sealed record MepQuote(decimal Buy, decimal Sell, DateTimeOffset UpdatedAt, string Source);

public interface IExchangeRateProvider
{
    Task<MepQuote> GetMepQuoteAsync(CancellationToken cancellationToken = default);
}
