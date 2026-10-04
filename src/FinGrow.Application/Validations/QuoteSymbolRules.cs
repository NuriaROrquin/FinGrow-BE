namespace FinGrow.Application.Validations;

using System.Text.RegularExpressions;
using Domain.Entities;
using Domain.Enums;

internal static partial class QuoteSymbolRules
{
    public static bool IsValid(PriceMarket market, string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        var normalized = market.NormalizeSymbol(symbol);

        return market == PriceMarket.MutualFund
            ? normalized.Length <= Investment.MaxFundNameLength
            : TickerPattern().IsMatch(normalized);
    }

    public static string InvalidMessage(PriceMarket market) =>
        market == PriceMarket.MutualFund
            ? $"El nombre del fondo es obligatorio y no puede superar los {Investment.MaxFundNameLength} caracteres."
            : $"El simbolo solo puede tener letras y numeros, hasta {Investment.MaxSymbolLength} caracteres.";

    public static bool IsQuotableCurrency(PriceMarket market, Currency currency) =>
        market == PriceMarket.MutualFund
            ? Enum.IsDefined(currency)
            : currency is Currency.ARS or Currency.USD;

    public static string UnquotableCurrencyMessage(PriceMarket market) =>
        market switch
        {
            PriceMarket.MutualFund => "La moneda no es valida.",
            PriceMarket.Crypto => "Las criptomonedas se cotizan en pesos o en dolares: elegi ARS o USD.",
            _ => "BYMA cotiza en pesos o en dolares: elegi ARS o USD.",
        };

    [GeneratedRegex("^[A-Z0-9]{1,20}$")]
    private static partial Regex TickerPattern();
}
