namespace FinGrow.Application.DTOs;

using Domain.Enums;
using Interfaces;

public sealed record MepQuoteResponse(
    Currency BaseCurrency,
    Currency QuoteCurrency,
    decimal Buy,
    decimal Sell,
    DateTimeOffset UpdatedAt,
    string Source)
{
    public static MepQuoteResponse FromQuote(MepQuote quote) => new(
        Currency.USD,
        Currency.ARS,
        quote.Buy,
        quote.Sell,
        quote.UpdatedAt,
        quote.Source);
}
