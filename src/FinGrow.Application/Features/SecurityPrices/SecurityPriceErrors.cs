namespace FinGrow.Application.Features.SecurityPrices;

using Common;
using Domain.Entities;
using Domain.Enums;

internal static class SecurityPriceErrors
{
    public static Error NotQuoted() => Error.Validation("SecurityPrice.NotQuoted", Investment.NotQuotedMessage);

    public static Error NotFound(PriceMarket market, string source, string symbol, Currency currency) =>
        Error.NotFound(
            "SecurityPrice.NotFound",
            market switch
            {
                PriceMarket.MutualFund =>
                    $"{source} no publica el fondo {symbol}. Elegi el nombre tal como aparece en la lista de fondos.",
                PriceMarket.Crypto =>
                    $"{source} no tiene cotizacion de {symbol} en {currency}. Revisa el simbolo: BTC, ETH, USDT.",
                _ =>
                    $"{source} no tiene cotizacion de {symbol} en {currency}. Revisa el simbolo o usa la variante de la moneda: AL30 en pesos, AL30D en dolares.",
            });

    public static Error Unavailable(string source) =>
        Error.Unavailable(
            "SecurityPrice.Unavailable",
            $"No pudimos obtener la cotizacion de {source}. Proba de nuevo en unos minutos.");
}
